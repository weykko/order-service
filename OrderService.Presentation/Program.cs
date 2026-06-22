using OrderService.Infrastructure.Migrations;
using OrderService.Presentation;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>())
    .Build()
    .RunMigrations()
    .Run();
