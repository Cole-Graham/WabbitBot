using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using WabbitBot.Analyzers.Descriptors;

namespace WabbitBot.Analyzers.Analyzers;

/// <summary>
/// Analyzer for EventBoundary attribute usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventBoundaryAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            EventAnalyzerDescriptors.EventBoundaryOnRecord,
            EventAnalyzerDescriptors.MissingEventBusInjection,
            EventAnalyzerDescriptors.CrudEventDetected,
            EventAnalyzerDescriptors.EventWithoutIEvent);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRecordDeclaration, SyntaxKind.RecordDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

        if (classSymbol == null)
            return;

        // Check for EventBoundary attribute
        if (HasEventBoundaryAttribute(classSymbol))
        {
            // Check for event bus injection
            CheckEventBusInjection(context, classDecl, classSymbol);

            // Check for CRUD patterns in class name
            CheckCrudPattern(context, classSymbol);
        }

        // Check if class ends with "Event" but doesn't implement IEvent
        if (classSymbol.Name.EndsWith("Event") && !ImplementsIEvent(classSymbol))
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.EventWithoutIEvent,
                classDecl.Identifier.GetLocation(),
                classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeRecordDeclaration(SyntaxNodeAnalysisContext context)
    {
        var recordDecl = (RecordDeclarationSyntax)context.Node;
        var recordSymbol = context.SemanticModel.GetDeclaredSymbol(recordDecl);

        if (recordSymbol == null)
            return;

        // Check for EventBoundary on record
        if (HasEventBoundaryAttribute(recordSymbol))
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.EventBoundaryOnRecord,
                recordDecl.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        // Check if record ends with "Event" but doesn't implement IEvent
        if (recordSymbol.Name.EndsWith("Event") && !ImplementsIEvent(recordSymbol))
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.EventWithoutIEvent,
                recordDecl.Identifier.GetLocation(),
                recordSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool HasEventBoundaryAttribute(INamedTypeSymbol symbol)
    {
        return symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "WabbitBot.Common.Attributes.EventBoundaryAttribute");
    }

    private bool ImplementsIEvent(INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(i => i.ToDisplayString() == "WabbitBot.Common.Events.EventInterfaces.IEvent");
    }

    private void CheckEventBusInjection(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDecl, INamedTypeSymbol classSymbol)
    {
        // Check constructor parameters for event bus
        var constructors = classSymbol.Constructors;
        bool hasEventBusInjection = constructors.Any(ctor =>
            ctor.Parameters.Any(param =>
                param.Type.ToDisplayString() == "WabbitBot.Common.Events.EventInterfaces.ICoreEventBus" ||
                param.Type.ToDisplayString() == "WabbitBot.Common.Events.EventInterfaces.IDiscordEventBus"));

        if (!hasEventBusInjection)
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.MissingEventBusInjection,
                classDecl.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void CheckCrudPattern(SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol)
    {
        var name = classSymbol.Name;
        if (name.Contains("Created") || name.Contains("Updated") || name.Contains("Deleted") ||
            name.Contains("Added") || name.Contains("Removed") || name.Contains("Archived"))
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.CrudEventDetected,
                context.Node.GetLocation(),
                name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
