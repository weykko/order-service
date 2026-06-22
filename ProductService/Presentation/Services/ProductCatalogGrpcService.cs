using Application.Exceptions;
using Application.Ports.Input;
using Grpc.Core;

namespace Presentation.Services
{
    /// <summary>
    /// gRPC-реализация каталога продуктов для межсервисного взаимодействия.
    /// Предоставляет системе заказов синхронные операции получения товара и резервирования склада.
    /// </summary>
    public class ProductCatalogGrpcService : global::ProductCatalog.Grpc.ProductCatalog.ProductCatalogBase
    {
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly ILogger<ProductCatalogGrpcService> _logger;

        public ProductCatalogGrpcService(
            IProductService productService,
            IStockService stockService,
            ILogger<ProductCatalogGrpcService> logger)
        {
            _productService = productService;
            _stockService = stockService;
            _logger = logger;
        }

        public override async Task<global::ProductCatalog.Grpc.ProductReply> GetProduct(
            global::ProductCatalog.Grpc.GetProductRequest request, ServerCallContext context)
        {
            var productId = ParseId(request.ProductId);

            try
            {
                var product = await _productService.GetByIdAsync(productId, context.CancellationToken);
                return new global::ProductCatalog.Grpc.ProductReply
                {
                    Found = true,
                    Id = product.Id.ToString(),
                    Name = product.Name,
                    Price = (double)product.Price,
                    Currency = product.Currency,
                    AvailableStock = product.AvailableStock,
                    IsInStock = product.IsInStock
                };
            }
            catch (NotFoundException)
            {
                return new global::ProductCatalog.Grpc.ProductReply { Found = false };
            }
        }

        public override async Task<global::ProductCatalog.Grpc.ReserveStockReply> ReserveStock(
            global::ProductCatalog.Grpc.ReserveStockRequest request, ServerCallContext context)
        {
            if (request.Quantity <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be positive"));

            var productId = ParseId(request.ProductId);
            var reserved = await _stockService.TryReserveStockAsync(productId, request.Quantity, context.CancellationToken);

            _logger.LogInformation("gRPC ReserveStock for {ProductId} x{Quantity}: {Result}",
                productId, request.Quantity, reserved);

            return new global::ProductCatalog.Grpc.ReserveStockReply { Reserved = reserved };
        }

        private static Guid ParseId(string rawId)
        {
            if (!Guid.TryParse(rawId, out var id))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product id format"));

            return id;
        }
    }
}
