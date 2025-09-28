using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.Analyzers.Descriptors;
using WabbitBot.Generator.Shared;
using WabbitBot.Generator.Shared.Analyzers;
using WabbitBot.Generator.Shared.Metadata;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Generators.Event
{
    /// <summary>
    /// Generates event boundary code for classes marked with [EventBoundary].
    /// Creates event records, publishers, and subscribers based on class methods.
    /// </summary>
    [Generator]
    public class EventBoundaryGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Pipeline: Find classes with [EventBoundary] attribute
            var eventBoundaryClasses = context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: (node, _) => node.HasAttribute("EventBoundary"),
                transform: (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    if (classSymbol == null)
                        return null;

                    var boundaryInfo = AttributeAnalyzer.ExtractEventBoundaryInfo(classSymbol);
                    if (boundaryInfo == null)
                        return null;

                    // Get event-triggering methods
                    var methods = AttributeAnalyzer.GetEventTriggeringMethods(classSymbol);
                    var methodInfos = methods.Select(m => new MethodInfo(
            Name: m.Name,
            Parameters: InferenceHelpers.ExtractEventParameters(m).ToList(),
            ReturnType: m.ReturnType.ToDisplayString()))
            .ToList();

                    return new EventBoundaryClassInfo(boundaryInfo, methodInfos);
                })
                                    .Where(info => info != null)
                                    .Collect();

            // Generate event records
            var eventRecords = eventBoundaryClasses
                .SelectMany((classes, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return classes.Where(c => c != null).SelectMany(c => GenerateEventRecords(c!));
                })
                .Collect();

            // Generate partial classes with publishers
            var partialClasses = eventBoundaryClasses
                .SelectMany((classes, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return classes.Where(c => c != null).Select(c => GeneratePartialClass(c!));
                })
                .Collect();

            // Register outputs
            context.RegisterSourceOutput(eventRecords, (spc, records) =>
            {
                foreach (var record in records)
                {
                    spc.AddSource(record.FileName, record.Content);
                }
            });

            context.RegisterSourceOutput(partialClasses, (spc, classes) =>
            {
                foreach (var cls in classes)
                {
                    spc.AddSource(cls.FileName, cls.Content);
                }
            });
        }

        private IEnumerable<(string FileName, SourceText Content)> GenerateEventRecords(EventBoundaryClassInfo classInfo)
        {
            var records = new List<(string, SourceText)>();

            foreach (var method in classInfo.Methods)
            {
                var eventName = InferenceHelpers.InferEventName(method.Name);
                var eventRecord = CommonTemplates.CreateEventRecord(
                    eventName,
                    method.Parameters,
                    classInfo.BoundaryInfo.BusType?.ToString() ?? "EventBusType.Core");

                var content = $$"""
                    {{CommonTemplates.CreateFileHeader("EventBoundaryGenerator")}}
                    using System;
                    using WabbitBot.Common.Events.EventInterfaces;

                    namespace WabbitBot.Core.Common.Events
                    {
                    {{eventRecord}}
                    }
                    """;

                records.Add(($"{eventName}.g.cs", content.ToSourceText()));
            }

            return records;
        }

        private (string FileName, SourceText Content) GeneratePartialClass(EventBoundaryClassInfo classInfo)
        {
            var className = classInfo.BoundaryInfo.ClassName;
            var busType = classInfo.BoundaryInfo.BusType ?? EventBusType.Core;

            var content = EventTemplates.GeneratePublisher(className, classInfo.Methods, busType);
            return ($"{className}.EventBoundary.g.cs", content);
        }

        private record EventBoundaryClassInfo(
            EventBoundaryInfo BoundaryInfo,
            List<MethodInfo> Methods);

        public record MethodInfo(
            string Name,
            List<(string type, string name)> Parameters,
            string ReturnType);
    }
}
