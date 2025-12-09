using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SharedService.Core.Database;
using SharedService.SharedKernel;

namespace FileService.Infrastructure.Postgres.Database;

public class TransactionManager : ITransactionManager
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TransactionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public TransactionManager(
        AppDbContext dbContext,
        ILogger<TransactionManager> logger,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<Result<ITransactionScope, Error>> BeginTransactionAsync(
        CancellationToken cancellationToken = new())
    {
        try
        {
            IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            ILogger<TransactionScope> logger = _loggerFactory.CreateLogger<TransactionScope>();

            TransactionScope transactionScope = new(transaction.GetDbTransaction(), logger);

            return transactionScope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction could not be started");

            return GeneralErrors.Failure("Transaction could not be started");
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsyncWithResult(CancellationToken cancellationToken = new())
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database update error");
            return GeneralErrors.Failure(ex.Message);
        }
    }
}