﻿Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language
Imports Wasm.Symbols
Imports Wasm.Symbols.Parser

Public Module Extensions

    Public Function CreateModule(vbcode As String) As ModuleSymbol
        Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText(vbcode)
        Dim root As CompilationUnitSyntax = tree.GetRoot
        Dim main As ModuleBlockSyntax = root.Members(Scan0)
        Dim functions As New List(Of FuncSymbol)
        Dim exports As New List(Of ExportSymbolExpression)
        Dim symbolTable As New SymbolTable(main.Members.OfType(Of MethodBlockSyntax))

        For Each method In main.Members.OfType(Of MethodBlockSyntax)
            functions += method.Parse(symbolTable)
            symbolTable.ClearLocals()

            If method.SubOrFunctionStatement.isExportObject Then
                exports += New ExportSymbolExpression With {
                    .Name = functions.Last.Name.Trim("$"c),
                    .target = functions.Last.Name,
                    .type = "func"
                }
            End If
        Next

        Return New ModuleSymbol With {
            .InternalFunctions = functions,
            .LabelName = main.ModuleStatement.Identifier.Text,
            .Exports = exports
        }
    End Function

    <Extension>
    Private Function isExportObject(method As MethodStatementSyntax) As Boolean
        If method.Modifiers.Count = 0 Then
            ' by default is public in VB.NET
            Return True
        Else
            For i As Integer = 0 To method.Modifiers.Count - 1
                If method.Modifiers _
                    .Item(i) _
                    .ValueText _
                    .TextEquals("Public") Then

                    Return True
                End If
            Next

            Return False
        End If
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    <Extension>
    Friend Function param(a As NamedValue(Of String)) As String
        Return $"(param ${a.Name} {a.Value})"
    End Function
End Module