using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using Utf8Json;

namespace CacheAPI.Helpers
{
    //// referenced from https://michaelscodingspot.com/the-battle-of-c-to-json-serializers-in-net-core-3/
    //internal sealed class Utf8JsonInputFormatter : IInputFormatter
    //{
    //    private readonly IJsonFormatterResolver _resolver;

    //    public Utf8JsonInputFormatter() : this(null) { }
    //    public Utf8JsonInputFormatter(IJsonFormatterResolver resolver)
    //    {
    //        _resolver = resolver ?? JsonSerializer.DefaultResolver;
    //    }

    //    public bool CanRead(InputFormatterContext context) => context.HttpContext.Request.ContentType.StartsWith("application/json");

    //    public async Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
    //    {
    //        var request = context.HttpContext.Request;

    //        if (request.Body.CanSeek && request.Body.Length == 0)
    //            return await InputFormatterResult.NoValueAsync();

    //        var result = await JsonSerializer.NonGeneric.DeserializeAsync(context.ModelType, request.Body, _resolver);
    //        return await InputFormatterResult.SuccessAsync(result);
    //    }
    //}

    //internal sealed class Utf8JsonOutputFormatter : IOutputFormatter
    //{
    //    private readonly IJsonFormatterResolver _resolver;

    //    public Utf8JsonOutputFormatter() : this(null) { }
    //    public Utf8JsonOutputFormatter(IJsonFormatterResolver resolver)
    //    {
    //        _resolver = resolver ?? JsonSerializer.DefaultResolver;
    //    }

    //    public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;


    //    public async Task WriteAsync(OutputFormatterWriteContext context)
    //    {
    //        if (!context.ContentTypeIsServerDefined)
    //            context.HttpContext.Response.ContentType = "application/json";

    //        if (context.ObjectType == typeof(object))
    //        {
    //            await JsonSerializer.NonGeneric.SerializeAsync(context.HttpContext.Response.Body, context.Object, _resolver);
    //        }
    //        else
    //        {
    //            await JsonSerializer.NonGeneric.SerializeAsync(context.ObjectType, context.HttpContext.Response.Body, context.Object, _resolver);
    //        }
    //    }
    //}
}
