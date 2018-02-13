using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static SimpleSourceProtector.Program;

namespace SimpleSourceProtector
{
    class Loader
    {
        public int Process(string projectPath, string outputPath, bool compileBefore = false, ObfuscationOptions obfuscationOptions = ObfuscationOptions.ALL, bool single = true, bool minify = true)
        {
            #region Creating new project from exist
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            string projName = "ObfuscatedProject";
            var projectId = ProjectId.CreateNewId();
            var versionStamp = VersionStamp.Create();
            var projectInfo = ProjectInfo.Create(projectId, versionStamp, projName, projName, LanguageNames.CSharp);
            var solution = workspace.CurrentSolution.AddProject(projectInfo);

            using (Watcher.Start(ts => Console.WriteLine("Creating project took: " + ts.ToString() + "\n")))
            {
                MSBuildWorkspace wspLoading = MSBuildWorkspace.Create();
                var prjLoading = wspLoading.OpenProjectAsync(projectPath).Result;
                Solution slnLoading = wspLoading.CurrentSolution;

                solution = solution.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                foreach (var prjId in slnLoading.GetProjectDependencyGraph().GetTopologicallySortedProjects())
                {
                    var prj = slnLoading.GetProject(prjId);

                    foreach (var doc in prj.Documents)
                        solution = solution.AddDocument(DocumentId.CreateNewId(projectId), doc.Name, doc.GetTextAsync().Result);
                    solution = solution.AddMetadataReferences(projectId, prj.MetadataReferences);
                }

                Console.WriteLine("Solution structure: ");
                foreach (var prj in solution.Projects)
                {
                    Console.WriteLine($"  Project: {prj.Id} Docs: {prj.Documents.Count()}");
                    foreach (var doc in prj.Documents)
                    {
                        Console.WriteLine($"    Document: {doc.Id} name: {doc.Name}");
                    }
                }
            }
            #endregion

            #region Check compiling test
            using (Watcher.Start(ts => Console.WriteLine("Check compilling took: " + ts.ToString() + "\n")))
            {
                if (compileBefore)
                {
                    using (var stream = new MemoryStream())
                    {
                        Console.WriteLine("--------------------------------CHECK COMPILING----------------------------------");
                        var result = solution.GetProject(projectId).GetCompilationAsync().Result.Emit(stream);
                        Console.WriteLine("Diagnostic: " + string.Join("\n", result.Diagnostics.Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error).Select(x => x.Id + " " + x.GetMessage() + " " + x.Location.GetLineSpan())));
                        Console.WriteLine("Compiling Result: " + result.Success);
                        Console.WriteLine("--------------------------------------------------------------------------");
                        if (!result.Success)
                            return 2;
                    }


                }
            }
            #endregion

            #region Renaming
            var documents = solution.Projects.SelectMany(x => x.Documents).Select(x => x.Id).ToList();
            if (obfuscationOptions != ObfuscationOptions.NONE)
                using (Watcher.Start(ts => Console.WriteLine("Renaming took: " + ts.ToString() + "\n")))
                    solution = ObfuscateNames(solution, documents, obfuscationOptions);
            #endregion

            #region Second comiling test

            var outWorkspace = workspace;
            var outSolution = solution;

            if (single)
            {
                outWorkspace = MSBuildWorkspace.Create();

                projName = "ObfuscatedSingleProject";
                projectId = ProjectId.CreateNewId();
                versionStamp = VersionStamp.Create();
                projectInfo = ProjectInfo.Create(projectId, versionStamp, projName, projName, LanguageNames.CSharp);
                outSolution = workspace.CurrentSolution.AddProject(projectInfo);
                outSolution = outSolution.WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                List<Document> docs = new List<Document>();
                foreach (var prjId in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects())
                {
                    var prj = solution.GetProject(prjId);
                    docs.AddRange(prj.Documents);
                    outSolution = outSolution.AddMetadataReferences(projectId, prj.MetadataReferences);
                }
                outSolution = outSolution.AddDocument(DocumentId.CreateNewId(projectId), Path.GetFileNameWithoutExtension(projectPath), ToSingleDoc(docs, outputPath));
                documents = outSolution.Projects.SelectMany(x => x.Documents).Select(x => x.Id).ToList();

            }

            using (Watcher.Start(ts => Console.WriteLine("Post compilling took: " + ts.ToString() + "\n")))
            {
                using (var stream = new MemoryStream())
                {
                    Console.WriteLine("--------------------------------POST COMPILING----------------------------------");
                    var res = outSolution.GetProject(projectId).GetCompilationAsync().Result.Emit(stream);
                    Console.WriteLine("Diagnostic: " + string.Join("\n", res.Diagnostics.Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error).Select(x => x.Id + " " + x.GetMessage() + " " + x.Location.GetLineSpan())));
                    Console.WriteLine("Compiling Result: " + res.Success);
                    Console.WriteLine("--------------------------------END COMPILING--------------------------------");

                    if (!res.Success)
                        return 3;
                }
            }
            #endregion

            #region Writing to output new project with deformating

            using (Watcher.Start(ts => Console.WriteLine("Writing to disk took: " + ts.ToString() + "\n")))
            {
                if (Directory.Exists(outputPath))
                {
                    Console.WriteLine($"Cleaning output dir: {outputPath}");
                    foreach (string file in Directory.GetFiles(outputPath, "*.cs"))
                        File.Delete(file);
                }
                else
                {
                    Console.WriteLine($"Creating output dir: {outputPath}");
                    Directory.CreateDirectory(outputPath);
                }

                foreach (var documentId in documents)
                {
                    var document = outSolution.GetDocument(documentId);
                    if (document.Name.Contains(".NETFramework")) continue;

                    var model = document.GetSemanticModelAsync().Result;
                    var syntax = document.GetSyntaxRootAsync().Result;

                    Rewriter rewriter = new Rewriter(model);
                    SyntaxNode newSource = (minify) ? rewriter.Visit(syntax) : syntax;

                    var newPath = Path.Combine(outputPath, (((obfuscationOptions & ObfuscationOptions.FILENAMES) == 0) ? document.Name : Utils.RandomString()) + ".cs");
                    Console.WriteLine("WRITING: " + document.Name + " as " + newPath);
                    File.WriteAllText(newPath, (minify) ? Utils.Unformat(newSource.ToFullString()) : newSource.ToFullString());
                }
            }

            #endregion

            return 0;
        }

        private static Solution ObfuscateNames(Solution solution, List<DocumentId> documents, ObfuscationOptions obfuscationOptions)
        {
            #region RENAMER

            #region Rename classes
            if ((obfuscationOptions & ObfuscationOptions.CLASS) != 0)
            {
                Console.WriteLine("----------------------------------CLASSES----------------------------------");
                using (Watcher.Start(ts => Console.WriteLine("Timed: " + ts.ToString())))
                {

                    foreach (var documentId in documents)
                    {
                        while (true)
                        {
                            var doc = solution.GetDocument(documentId);
                            var model = doc.GetSemanticModelAsync().Result;
                            var syntax = doc.GetSyntaxRootAsync().Result;
                            var classes = syntax.DescendantNodes()
                              .OfType<ClassDeclarationSyntax>()
                              .Where(x => !x.Identifier.ValueText.StartsWith("_") && x.Identifier.ValueText.IndexOf("ignore", StringComparison.OrdinalIgnoreCase) < 0)
                              .ToList();

                            var cl = classes.FirstOrDefault();
                            if (cl == null)
                                break;
                            var symbol = model.GetDeclaredSymbol(cl);
                            var newName = "_" + Utils.RandomString();
                            Console.WriteLine("Renaming class: " + cl.Identifier.ValueText + " to " + newName);
                            solution = Renamer.RenameSymbolAsync(solution, symbol, newName, null).Result;
                        }

                    }
                }
            }
            #endregion

            #region Rename methods
            if ((obfuscationOptions & ObfuscationOptions.METHODS) != 0)
            {
                Console.WriteLine("----------------------------------METHODS----------------------------------");
                using (Watcher.Start(ts => Console.WriteLine("Timed: " + ts.ToString())))
                {
                    foreach (var documentId in documents)
                    {
                        List<MethodDeclarationSyntax> methods;
                        int i;
                        do
                        {
                            var doc = solution.GetDocument(documentId);
                            var model = doc.GetSemanticModelAsync().Result;
                            var syntax = doc.GetSyntaxRootAsync().Result;
                            methods = syntax.DescendantNodes()
                              .OfType<MethodDeclarationSyntax>()
                              .Where(x => !x.Identifier.ValueText.StartsWith("_") && !x.Identifier.ToString().Equals("dispose", StringComparison.OrdinalIgnoreCase) && x.Modifiers.Count(z => z.IsKind(SyntaxKind.ProtectedKeyword) || z.IsKind(SyntaxKind.OverrideKeyword)) == 0)
                              .ToList();

                            for (i = 0; i < methods.Count; i++)
                            {
                                var ms = methods[i];
                                var symbol = model.GetDeclaredSymbol(ms);

                                if (ms.GetLeadingTrivia().ToString().IndexOf("ignore", StringComparison.OrdinalIgnoreCase) >= 0)
                                    continue;

                                var refcount = 0;
                                foreach (var rf in SymbolFinder.FindReferencesAsync(symbol, doc.Project.Solution).Result)
                                    refcount += rf.Locations.Count();
                                if (refcount <= 0) continue;

                                var newName = "_" + Utils.RandomString();
                                Console.WriteLine("Renaming method (" + refcount + "): " + ms.Identifier.ValueText + " to " + newName + $" {ms.Kind()} {string.Join(",", ms.Modifiers)}");
                                solution = Renamer.RenameSymbolAsync(solution, symbol, newName, null).Result;
                                break;
                            }
                        } while (i < methods.Count);
                    }
                }
            }
            #endregion

            #region Rename variables
            if ((obfuscationOptions & ObfuscationOptions.VARS) != 0)
            {
                Console.WriteLine("----------------------------------VARS&FIELDS----------------------------------");
                using (Watcher.Start(ts => Console.WriteLine("Timed: " + ts.ToString())))
                {
                    foreach (var documentId in documents)
                    {
                        List<VariableDeclarationSyntax> vars;
                        int i;
                        do
                        {
                            var doc = solution.GetDocument(documentId);
                            var model = doc.GetSemanticModelAsync().Result;
                            var syntax = doc.GetSyntaxRootAsync().Result;
                            vars = syntax.DescendantNodes()
                              .OfType<VariableDeclarationSyntax>()
                              .Where(x => x.Variables.Count(z => !z.Identifier.ValueText.StartsWith("_")) > 0)
                              .ToList();

                            for (i = 0; i < vars.Count; i++)
                            {
                                bool end = true;
                                foreach (var vr in vars[i].Variables)
                                {
                                    if (vr.Identifier.ValueText.StartsWith("_")) continue;

                                    var symbol = model.GetDeclaredSymbol(vr);

                                    if (vr.GetLeadingTrivia().ToString().IndexOf("ignore", StringComparison.OrdinalIgnoreCase) >= 0)
                                        continue;

                                    var newName = "_" + Utils.RandomString();
                                    Console.WriteLine("Renaming variable: " + vr.Identifier.ValueText + " to " + newName + $" {vr.Kind()}");
                                    solution = Renamer.RenameSymbolAsync(solution, symbol, newName, null).Result;
                                    end = true;
                                    break;
                                }
                                if (end) break;
                            }
                        } while (i < vars.Count);
                    }
                }
            }
            #endregion

            if ((obfuscationOptions & ObfuscationOptions.OTHERS) != 0)
            {
                #region Rename Enums
                Console.WriteLine("----------------------------------ENUMS----------------------------------");
                using (Watcher.Start(ts => Console.WriteLine("Timed: " + ts.ToString())))
                {
                    foreach (var documentId in documents)
                    {
                        List<EnumDeclarationSyntax> vars;
                        int i;
                        do
                        {
                            var doc = solution.GetDocument(documentId);
                            var model = doc.GetSemanticModelAsync().Result;
                            var syntax = doc.GetSyntaxRootAsync().Result;
                            vars = syntax.DescendantNodes()
                              .OfType<EnumDeclarationSyntax>()
                              .Where(x => !x.Identifier.ValueText.StartsWith("_"))
                              .ToList();

                            for (i = 0; i < vars.Count; i++)
                            {
                                var vr = vars[i];
                                if (vr.Identifier.ValueText.StartsWith("_")) continue;

                                var symbol = model.GetDeclaredSymbol(vr);

                                if (vr.GetLeadingTrivia().ToString().IndexOf("ignore", StringComparison.OrdinalIgnoreCase) >= 0)
                                    continue;

                                var newName = "_" + Utils.RandomString();
                                Console.WriteLine("Renaming ENUM: " + vr.Identifier.ValueText + " to " + newName + $" {vr.Kind()}");
                                solution = Renamer.RenameSymbolAsync(solution, symbol, newName, null).Result;
                                break;
                            }
                        } while (i < vars.Count);
                    }
                }

                Console.WriteLine("----------------------------------ENUM MEMBERS----------------------------------");
                using (Watcher.Start(ts => Console.WriteLine("Timed: " + ts.ToString())))
                {
                    foreach (var documentId in documents)
                    {
                        List<EnumMemberDeclarationSyntax> vars;
                        int i;
                        do
                        {
                            var doc = solution.GetDocument(documentId);
                            var model = doc.GetSemanticModelAsync().Result;
                            var syntax = doc.GetSyntaxRootAsync().Result;
                            vars = syntax.DescendantNodes()
                              .OfType<EnumMemberDeclarationSyntax>()
                              .Where(x => !x.Identifier.ValueText.StartsWith("_"))
                              .ToList();

                            for (i = 0; i < vars.Count; i++)
                            {
                                var vr = vars[i];
                                if (vr.Identifier.ValueText.StartsWith("_")) continue;

                                var symbol = model.GetDeclaredSymbol(vr);

                                if (vr.GetLeadingTrivia().ToString().IndexOf("ignore", StringComparison.OrdinalIgnoreCase) >= 0)
                                    continue;

                                var newName = "_" + Utils.RandomString();
                                Console.WriteLine("Renaming ENUM member: " + vr.Identifier.ValueText + " to " + newName + $" {vr.Kind()}");
                                solution = Renamer.RenameSymbolAsync(solution, symbol, newName, null).Result;
                                break;
                            }
                        } while (i < vars.Count);
                    }
                }

                #endregion

                #region Rename Structs
                Console.WriteLine("----------------------------------STRUCTS----------------------------------");
                using (Watcher.Start(ts => Console.WriteLine("Timed: " + ts.ToString())))
                {
                    foreach (var documentId in documents)
                    {
                        List<StructDeclarationSyntax> vars;
                        int i;
                        do
                        {
                            var doc = solution.GetDocument(documentId);
                            var model = doc.GetSemanticModelAsync().Result;
                            var syntax = doc.GetSyntaxRootAsync().Result;
                            vars = syntax.DescendantNodes()
                              .OfType<StructDeclarationSyntax>()
                              .Where(x => !x.Identifier.ValueText.StartsWith("_"))
                              .ToList();

                            for (i = 0; i < vars.Count; i++)
                            {
                                var vr = vars[i];
                                if (vr.Identifier.ValueText.StartsWith("_")) continue;

                                var symbol = model.GetDeclaredSymbol(vr);

                                if (vr.GetLeadingTrivia().ToString().IndexOf("ignore", StringComparison.OrdinalIgnoreCase) >= 0)
                                    continue;

                                var newName = "_" + Utils.RandomString();
                                Console.WriteLine("Renaming STRUCTS: " + vr.Identifier.ValueText + " to " + newName + $" {vr.Kind()}");
                                solution = Renamer.RenameSymbolAsync(solution, symbol, newName, null).Result;
                                break;
                            }
                        } while (i < vars.Count);
                    }
                }
                #endregion
            }

            return solution;
            #endregion
        }


        private string ToSingleDoc(IEnumerable<Document> documents, string outputPath)
        {
            List<string> sources = new List<string>();
            foreach (var document in documents)
            {
                if (document.Name.Contains(".NETFramework")) continue;

                var model = document.GetSemanticModelAsync().Result;
                var syntax = document.GetSyntaxRootAsync().Result;

                UsingRewriter rewriter = new UsingRewriter(model);
                SyntaxNode newSource = rewriter.Visit(syntax);

                sources.Add(newSource.ToFullString());
            }

            StringBuilder result = new StringBuilder();

            var list = sources.ToList();
            list.Shuffle();
            foreach (var src in list)
                result.AppendLine(src);

            return result.ToString();
        }
    }
}
