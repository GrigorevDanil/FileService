using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets.ValueObjects;

public sealed record MediaOwner
{
    private const int MAX_CONTEXT_LENGTH = 50;

    private static readonly HashSet<string> _allowedContexts =
    [
        "department"
    ];

    public string Context { get; }

    public Guid EntityId { get; }

    public MediaOwner(string context, Guid entityId)
    {
        Context = context;
        EntityId = entityId;
    }

    /// <summary>
    /// Создает новый объект <see cref="MediaOwner"/>.
    /// </summary>
    /// <param name="context">Контекст.</param>
    /// <param name="entityId">Идентификатор сущности.</param>
    /// <returns>Новый объект <see cref="MediaOwner"/> или ошибка <see cref="Error"/>.</returns>
    public static Result<MediaOwner, Error> Of(string context, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(context) || context.Length > MAX_CONTEXT_LENGTH)
            return GeneralErrors.ValueIsInvalid("mediaOwner.context");

        string normalizedContext = context.Trim().ToLowerInvariant();
        if (!_allowedContexts.Contains(normalizedContext))
            return GeneralErrors.ValueIsInvalid("mediaOwner.context");

        if (entityId == Guid.Empty)
            return GeneralErrors.ValueIsInvalid("mediaOwner.entityId");

        return new MediaOwner(normalizedContext, entityId);
    }

    public static Result<MediaOwner, Error> ForDepartment(Guid departmentId) => Of("department",  departmentId);
}