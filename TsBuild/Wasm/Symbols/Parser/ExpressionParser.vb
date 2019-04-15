﻿#Region "Microsoft.VisualBasic::312ea86adbdb926e29f152f8b6f7eb8b, Symbols\Parser\ExpressionParser.vb"

' Author:
' 
'       xieguigang (I@xieguigang.me)
' 
' Copyright (c) 2019 GCModeller Cloud Platform
' 
' 
' MIT License
' 
' 
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
' 
' The above copyright notice and this permission notice shall be included in all
' copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
' SOFTWARE.



' /********************************************************************************/

' Summaries:

'     Module ExpressionParse
' 
'         Function: [Select], (+2 Overloads) BinaryStack, ConstantExpression, FunctionInvoke, ParenthesizedStack
'                   ReferVariable, UnaryExpression, ValueExpression
' 
' 
' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel

Namespace Symbols.Parser

    Module ExpressionParse

        <Extension>
        Public Function ValueExpression(value As ExpressionSyntax, symbols As SymbolTable) As Expression
            Select Case value.GetType
                Case GetType(BinaryExpressionSyntax)
                    Return DirectCast(value, BinaryExpressionSyntax).BinaryStack(symbols)
                Case GetType(ParenthesizedExpressionSyntax)
                    Return DirectCast(value, ParenthesizedExpressionSyntax).ParenthesizedStack(symbols)
                Case GetType(LiteralExpressionSyntax)
                    Return DirectCast(value, LiteralExpressionSyntax).ConstantExpression(Nothing, symbols)
                Case GetType(IdentifierNameSyntax)
                    Return DirectCast(value, IdentifierNameSyntax).ReferVariable(symbols)
                Case GetType(InvocationExpressionSyntax)
                    Return DirectCast(value, InvocationExpressionSyntax).FunctionInvoke(symbols)
                Case GetType(UnaryExpressionSyntax)
                    Return DirectCast(value, UnaryExpressionSyntax).UnaryExpression(symbols)
                Case Else
                    Throw New NotImplementedException(value.GetType.FullName)
            End Select
        End Function

        <Extension>
        Public Function UnaryExpression(unary As UnaryExpressionSyntax, symbols As SymbolTable) As FuncInvoke
            Dim op$ = unary.OperatorToken.ValueText
            Dim right = unary.Operand.ValueExpression(symbols)
            Dim left = New LiteralExpression With {
                .type = right.TypeInfer(symbols),
                .value = 0
            }

            Return New FuncInvoke With {
                .Parameters = {left, right},
                .Reference = $"{left.type}.{Types.Operators(op)}",
                .[operator] = True
            }
        End Function

        <Extension>
        Public Iterator Function [Select](args As SeparatedSyntaxList(Of ArgumentSyntax), symbols As SymbolTable, params As NamedValue(Of String)()) As IEnumerable(Of Expression)
            For i As Integer = 0 To args.Count - 1
                Dim value As Expression = args.Item(i) _
                    .GetExpression _
                    .ValueExpression(symbols)
                Dim left$ = params(i).Value

                Yield Types.CType(left, value, symbols)
            Next
        End Function

        <Extension>
        Public Function FunctionInvoke(invoke As InvocationExpressionSyntax, symbols As SymbolTable) As FuncInvoke
            Dim reference = invoke.Expression
            Dim arguments As Expression()
            Dim funcName$

            ' 得到被调用的目标函数的名称符号
            Select Case reference.GetType
                Case GetType(SimpleNameSyntax)
                    funcName = DirectCast(reference, SimpleNameSyntax).objectName
                Case GetType(IdentifierNameSyntax)
                    funcName = DirectCast(reference, IdentifierNameSyntax).objectName
                Case Else
                    Throw New NotImplementedException(reference.GetType.FullName)
            End Select

            Dim funcDeclare = symbols.GetFunctionSymbol(funcName)

            If invoke.ArgumentList Is Nothing Then
                arguments = {}
            Else
                arguments = invoke.ArgumentList _
                    .Arguments _
                    .Select(symbols, funcDeclare.Parameters) _
                    .ToArray
            End If

            Return New FuncInvoke With {
                .Reference = funcName,
                .Parameters = arguments
            }
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <Extension>
        Public Function ReferVariable(name As IdentifierNameSyntax, symbols As SymbolTable) As Expression
            Dim var As String = name.objectName

            If symbols.IsLocal(var) Then
                Return New GetLocalVariable With {
                    .var = var
                }
            Else
                Return New GetGlobalVariable With {
                    .var = var
                }
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="[const]"></param>
        ''' <param name="wasmType"></param>
        ''' <param name="memory">内存设备</param>
        ''' <returns></returns>
        <Extension>
        Public Function ConstantExpression([const] As LiteralExpressionSyntax, wasmType$, memory As Memory) As Expression
            Dim value As Object = [const].Token.Value
            Dim type As Type = value.GetType

            If type Is GetType(String) OrElse type Is GetType(Char) Then
                ' 是字符串类型，需要做额外的处理
                value = memory.AddString(value)
                wasmType = "char*"
            ElseIf type Is GetType(Boolean) Then
                wasmType = "i32"
                value = If(DirectCast(value, Boolean), 1, 0)
            Else
                If wasmType.StringEmpty Then
                    wasmType = Types.Convert2Wasm(type)
                End If
            End If

            Return New LiteralExpression With {
                .type = wasmType,
                .value = value
            }
        End Function

        <Extension>
        Public Function ParenthesizedStack(parenthesized As ParenthesizedExpressionSyntax, symbols As SymbolTable) As Parenthesized
            Return New Parenthesized With {
                .Internal = parenthesized.Expression.ValueExpression(symbols)
            }
        End Function

        ''' <summary>
        ''' NOTE: div between two integer will convert to double div automatic. 
        ''' </summary>
        ''' <param name="expression"></param>
        ''' <param name="symbols"></param>
        ''' <returns></returns>
        <Extension>
        Public Function BinaryStack(expression As BinaryExpressionSyntax, symbols As SymbolTable) As Expression
            Dim left = expression.Left.ValueExpression(symbols)
            Dim right = expression.Right.ValueExpression(symbols)
            Dim op$ = expression.OperatorToken.ValueText

            Return BinaryStack(left, right, op, symbols)
        End Function

        ''' <summary>
        ''' NOTE: div between two integer will convert to double div automatic. 
        ''' </summary>
        ''' <param name="symbols"></param>
        ''' <returns></returns>
        Public Function BinaryStack(left As Expression, right As Expression, op$, symbols As SymbolTable) As Expression
            Dim type$

            If op = "/" Then
                ' require type conversion if left and right is integer
                ' 对于除法，必须要首先转换为浮点型才能够完成运算
                left = Types.CDbl(left, symbols)
                right = Types.CDbl(right, symbols)
                type = "f64"
            ElseIf op = "&" Then
                ' vb string concatenation
                If Not ImportSymbol.JsStringConcatenation.Ref Like symbols.Requires Then
                    symbols.Requires.Add(ImportSymbol.JsStringConcatenation.Ref)
                    symbols.AddImports(ImportSymbol.JsStringConcatenation)
                End If

                Return stringConcatenation(left, right)
            Else
                ' 其他的运算符则需要两边的类型保持一致
                ' 往高位转换
                ' i32 -> f32 -> i64 -> f64
                Dim lt = left.TypeInfer(symbols)
                Dim rt = right.TypeInfer(symbols)
                Dim li = Types.Orders.IndexOf(lt)
                Dim ri = Types.Orders.IndexOf(rt)

                If li > ri Then
                    type = lt
                Else
                    type = rt
                End If

                left = Types.CType(type, left, symbols)
                right = Types.CType(type, right, symbols)
            End If

            Dim funcOpName$

            If Types.Operators.ContainsKey(op) Then
                funcOpName = Types.Operators(op)
                funcOpName = $"{type}.{funcOpName}"
            Else
                funcOpName = Types.Compares(type, op)
            End If

            ' 需要根据类型来决定操作符函数的类型来源
            Return New FuncInvoke With {
                .Parameters = {left, right},
                .Reference = funcOpName,
                .[operator] = True
            }
        End Function

        Private Function stringConcatenation(left As Expression, right As Expression) As Expression
            Return New FuncInvoke With {
                .Parameters = {left, right},
                .Reference = ImportSymbol.JsStringConcatenation.Name,
                .[operator] = False
            }
        End Function
    End Module
End Namespace
