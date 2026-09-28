using Gridify;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Spot4Hire.Backend.Common;

namespace Spot4Hire.Backend.OpenApi;

// Documents the Gridify "filter" and "orderBy" query parameters on the list
// endpoints, including the fields each endpoint allows (read from the central
// mappers, so the docs never drift from what actually filters).
internal sealed class GridifyParameterTransformer : IOpenApiOperationTransformer
{
    static GridifyParameterTransformer()
    {
        FieldsByController = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Venues"] = Fields(GridifyMappers.Venue),
            ["Units"] = Fields(GridifyMappers.Unit),
            ["Bookings"] = Fields(GridifyMappers.Booking),
            ["Users"] = Fields(GridifyMappers.User),
            ["OpeningHours"] = Fields(GridifyMappers.OpeningHour),
        };
    }

    private static readonly Dictionary<string, string> FieldsByController;

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.Parameters is null)
        {
            return Task.CompletedTask;
        }

        string? allowed = null;
        if (context.Description.ActionDescriptor is ControllerActionDescriptor descriptor
            && FieldsByController.TryGetValue(descriptor.ControllerName, out var fields))
        {
            allowed = fields;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (parameter is not OpenApiParameter p)
            {
                continue;
            }

            if (string.Equals(p.Name, "filter", StringComparison.OrdinalIgnoreCase))
            {
                p.Description = FilterDescription(allowed);
            }
            else if (string.Equals(p.Name, "orderBy", StringComparison.OrdinalIgnoreCase))
            {
                p.Description = OrderByDescription(allowed);
            }
        }

        return Task.CompletedTask;
    }

    private static string Fields<T>(IGridifyMapper<T> mapper)
        => string.Join(", ", mapper.GetCurrentMaps().Select(m => m.From));

    private static string FilterDescription(string? allowed)
    {
        const string text =
            "Gridify filter. Operators: = equals, != not equals, =* contains, !* not contains, > < >= <=. "
            + "Combine with , for AND and | for OR. Example: name=*coffee,price>100000";
        return allowed is null ? text : $"{text}. Filterable fields: {allowed}";
    }

    private static string OrderByDescription(string? allowed)
    {
        const string text =
            "Gridify sort: comma-separated fields, each optionally followed by 'desc'. Example: createdAt desc, name";
        return allowed is null ? text : $"{text}. Sortable fields: {allowed}";
    }
}
