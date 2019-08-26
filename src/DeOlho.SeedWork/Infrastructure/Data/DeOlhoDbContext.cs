using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DeOlho.SeedWork.Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DeOlho.SeedWork.Infrastructure.Data
{
    public class DeOlhoDbContextConfiguration
    {
        public DeOlhoDbContextConfiguration(
            string connectionString,
            Assembly migrationAssembly,
            params Assembly[] entityConfigurationAssemblies)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            MigrationAssembly = migrationAssembly ?? throw new ArgumentNullException(nameof(migrationAssembly));
            entityConfigurationAssemblies = entityConfigurationAssemblies ?? new Assembly[] { migrationAssembly };
            if (!entityConfigurationAssemblies.Any()) entityConfigurationAssemblies = new Assembly[] { migrationAssembly };
            EntityConfigurationAssemblies = entityConfigurationAssemblies;
        }

        public string ConnectionString { get; set; }
        public Assembly MigrationAssembly { get; set; }
        public Assembly[] EntityConfigurationAssemblies { get; set; }
    }

    public class DeOlhoDbContext: DbContext, IUnitOfWork
    {
        readonly IMediator _mediator;
        readonly DeOlhoDbContextConfiguration _deOlhoDbContextConfiguration;

        public DeOlhoDbContext(
            DbContextOptions<DeOlhoDbContext> options, 
            IMediator mediator,
            DeOlhoDbContextConfiguration deOlhoDbContextConfiguration) : base(options)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _deOlhoDbContextConfiguration = deOlhoDbContextConfiguration ?? throw new ArgumentNullException(nameof(deOlhoDbContextConfiguration));
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                _deOlhoDbContextConfiguration.ConnectionString,
                c => c.MigrationsAssembly(_deOlhoDbContextConfiguration.MigrationAssembly.GetName().Name));
        }

        public override int SaveChanges()
        {
            _mediator.DispatchDomainEventsAsync(this).Wait();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            _mediator.DispatchDomainEventsAsync(this).Wait();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await _mediator.DispatchDomainEventsAsync(this);
            return await base.SaveChangesAsync(cancellationToken);
        }

        public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _mediator.DispatchDomainEventsAsync(this);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach(var assembly in _deOlhoDbContextConfiguration.EntityConfigurationAssemblies)
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}