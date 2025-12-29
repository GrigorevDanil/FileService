using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Core.HttpCommunication;

public static class HttpResponseMessageExtensions
{
    extension(HttpResponseMessage response)
    {
        public async Task<Result<TResponse, Errors>> HandleResponseAsync<TResponse>(CancellationToken cancellationToken = default)
        {
            try
            {
                Envelope<TResponse>? data =
                    await response.Content.ReadFromJsonAsync<Envelope<TResponse>>(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return data?.ErrorList ?? GeneralErrors.Failure("Unknown error");

                if (data is null)
                    return GeneralErrors.Failure("Error while reading response").ToErrors();

                if (data.ErrorList is not null)
                    return data.ErrorList;

                if (data.Result is null)
                    return GeneralErrors.Failure("Error while reading response").ToErrors();

                return data.Result;
            }
            catch
            {
                return GeneralErrors.Failure("Error while reading response").ToErrors();
            }
        }

        public async Task<UnitResult<Errors>> HandleResponseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Envelope? data = await response.Content.ReadFromJsonAsync<Envelope>(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return data?.ErrorList ?? GeneralErrors.Failure("Unknown error");

                if (data is null)
                    return GeneralErrors.Failure("Error while reading response").ToErrors();

                if (data.ErrorList is not null)
                    return data.ErrorList;

                return UnitResult.Success<Errors>();
            }
            catch
            {
                return GeneralErrors.Failure("Error while reading response").ToErrors();
            }
        }
    }
}