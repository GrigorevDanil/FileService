using System.Data;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using SharedService.Core.Database;
using SharedService.SharedKernel;

namespace FileService.Infrastructure.Postgres.Database;

public class TransactionScope : ITransactionScope
{
    private readonly IDbTransaction _transaction;
    private readonly ILogger<TransactionScope> _logger;

    public TransactionScope(IDbTransaction transaction, ILogger<TransactionScope> logger)
    {
        _transaction = transaction;
        _logger = logger;
    }

    public UnitResult<Error> Commit()
    {
        try
        {
            _transaction.Commit();
            return Result.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't commit");

            return GeneralErrors.Failure("Couldn't commit");
        }
    }

    public UnitResult<Error> Rollback()
    {
        try
        {
            _transaction.Rollback();
            return Result.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Couldn't rollback");

            return GeneralErrors.Failure("Couldn't rollback");
        }
    }

    public void Dispose() => _transaction.Dispose();
}