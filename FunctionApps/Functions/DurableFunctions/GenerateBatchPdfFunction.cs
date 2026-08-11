using DurableTask.Core;
using FunctionApps.Facade;
using FunctionApps.Functions.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks;

//Fan-out Fan-in pattern

namespace FunctionApps.Functions.DurableFunctions;
public class GenerateBatchPdfFunction
{
    private readonly IRetryPolicyProvider _retryPolicyProvider;

    // Constructor injection for IRetryPolicyProvider
    public GenerateBatchPdfFunction(IRetryPolicyProvider retryPolicyProvider)
    {
        _retryPolicyProvider = retryPolicyProvider;
    }

    [Function("GenerateBatchPdfFunction")]
    public async Task<HttpResponseData> HttpGenerateBatchPdfFunctionStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{customerId}/invoices/pdf")] HttpRequestData req,
        int customerId,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("GenerateBatchPdfFunction");
        logger.LogInformation("GenerateBatchPdfFunction called for {customerId}", customerId);

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                                            "InvoiceBatchOrchestrator", 
                                            new BatchInvoiceRequest { CustomerId = customerId }
                                        );

        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    [Function("InvoiceBatchOrchestrator")]
    public async Task<string> InvoiceBatchOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger("InvoiceBatchOrchestrator");
        logger.LogInformation("-----------InvoiceBatchOrchestrator called---------------");

        var request = context.GetInput<BatchInvoiceRequest>();

        if (request == null)
        {
            logger.LogInformation("Invalid request data");
            return string.Empty;
        }

        var retryOptions = _retryPolicyProvider.Get(Enum.RetryPolicyTypeEnum.PdfGeneration);

        var invoiceIds = await context.CallActivityAsync<List<int>>("GetInvoicesActivity", request.CustomerId);

        var tasks = invoiceIds.Select(id => CallGenerateInvoiceWithRetryAndCapture(context, id, retryOptions)).ToList();

        PdfGenerationResult[] pdfResults = await Task.WhenAll(tasks);

        var successfulPdfUrls = pdfResults.Where(r => r.Success && !string.IsNullOrEmpty(r.BlobUrl))
                                            .Select(r => r.BlobUrl!)
                                            .ToList();

        logger.LogWarning("Successful PDFs generated: {Count}", successfulPdfUrls.Count);

        var failed = pdfResults.Where(r => !r.Success).ToList();
        if (failed.Count > 0)
            logger.LogWarning("Some PDFs failed to generate: {Count}", failed.Count);

        var zipUrl = await context.CallActivityAsync<string>("CreateZipActivity", successfulPdfUrls.ToList());

        logger.LogInformation(zipUrl);

        return zipUrl;
    }

    [Function("GetInvoicesActivity")]
    public static async Task<List<int>> GetInvoicesActivity([ActivityTrigger] int customerId, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("GetInvoicesActivity");
        logger.LogInformation("GetInvoicesActivity called");

        await Task.CompletedTask;

        return new List<int>
        {
            1001,
            1002,
            1003,
            1004,
            1005
        };
    }

    private static async Task<PdfGenerationResult> CallGenerateInvoiceWithRetryAndCapture(TaskOrchestrationContext context, int invoiceId, RetryOptions retryOptions)
    {
        for (int attempt = 1; attempt <= retryOptions.MaxNumberOfAttempts; attempt++)
        {
            try
            {
                return await context.CallActivityAsync<PdfGenerationResult>("GenerateInvoicePdfActivity", invoiceId);
            }
            catch (Exception ex)
            {
                if (attempt == retryOptions.MaxNumberOfAttempts)
                {
                    return new PdfGenerationResult
                    {
                        InvoiceId = invoiceId,
                        ErrorMessage = ex.Message,
                        Success = false
                    };
                }

                double factor = Math.Pow(retryOptions.BackoffCoefficient, attempt - 1);
                var delay = TimeSpan.FromMilliseconds(Math.Min(retryOptions.MaxRetryInterval.TotalMilliseconds, 
                                                                retryOptions.FirstRetryInterval.TotalMilliseconds * factor));
                var nextFire = context.CurrentUtcDateTime.Add(delay);

                await context.CreateTimer(nextFire, CancellationToken.None);
            }
        }

        return new PdfGenerationResult { InvoiceId = invoiceId, Success = false, ErrorMessage = "Unknown retry failure" };
    }

    [Function("GenerateInvoicePdfActivity")]
    public static async Task<PdfGenerationResult> GenerateInvoicePdfActivity([ActivityTrigger] int invoiceId, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("GenerateInvoicePdfActivity");

        logger.LogInformation("GenerateInvoicePdfActivity called for invoice {InvoiceId}", invoiceId);

        //Fail specific invoiceid to test
        if (invoiceId == 1003)
            throw new TimeoutException($"Simulated transient failure for invoice {invoiceId}");

        await Task.CompletedTask;

        return new PdfGenerationResult
        {
            InvoiceId = invoiceId,
            Success = true,
            BlobUrl = $"https://fakestorage.local/pdfs/{invoiceId}.pdf"
        };

        //ILogger logger = executionContext.GetLogger("GenerateInvoicePdfActivity");

        //logger.LogInformation("GenerateInvoicePdfActivity called");

        //await Task.CompletedTask;

        //return new PdfGenerationResult
        //{
        //    InvoiceId = invoiceId,
        //    Success = true
        //};
    }

    [Function("CreateZipActivity")]
    public static async Task<string> CreateZipActivity([ActivityTrigger] List<string> pdfUrls, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("CreateZipActivity");
        logger.LogInformation($"-------Zip file created {string.Join("/", pdfUrls)}------------");

        await Task.CompletedTask;

        return string.Join("/", pdfUrls);
    }
}