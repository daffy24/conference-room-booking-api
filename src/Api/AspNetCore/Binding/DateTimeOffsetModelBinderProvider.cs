using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ConferenceBooking.Api.AspNetCore.Binding;

internal sealed class DateTimeOffsetModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.ModelType == typeof(DateTimeOffset) ? new DateTimeOffsetModelBinder() : null;
}

internal sealed class DateTimeOffsetModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        if (value.Length == 1 && DateTimeOffsetParser.TryParse(value.FirstValue, out var timestamp))
        {
            bindingContext.Result = ModelBindingResult.Success(timestamp);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, DateTimeOffsetParser.ErrorMessage);
        bindingContext.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }
}
