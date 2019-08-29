using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DeOlho.SeedWork.Domain.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

        public IDbContextTransaction CurrentTransaction => Database.CurrentTransaction;

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var hasTransaction = CurrentTransaction != null;
            if (!hasTransaction)
                Database.BeginTransaction();
                

            _mediator.DispatchDomainEventsAsync(this).Wait();

            var ret = base.SaveChanges(acceptAllChangesOnSuccess);

            if (!hasTransaction)
                Database.CommitTransaction();

            return ret;
        }

        public override int SaveChanges()
        {
            return SaveChanges(true);
        }

        public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            var hasTransaction = CurrentTransaction != null;
            if (!hasTransaction)
                await Database.BeginTransactionAsync(cancellationToken);
                

            await _mediator.DispatchDomainEventsAsync(this);

            var ret = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (!hasTransaction)
                Database.CommitTransaction();

            return ret;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return SaveChangesAsync(true, cancellationToken);
        }

 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach(var assembly in _deOlhoDbContextConfiguration.EntityConfigurationAssemblies)
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}