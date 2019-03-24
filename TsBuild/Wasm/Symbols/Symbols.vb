﻿Namespace Symbols

    ''' <summary>
    ''' 一般的函数调用表达式，也包括运算符运算
    ''' </summary>
    Public Class FuncInvoke : Inherits Expression

        ''' <summary>
        ''' Function reference string
        ''' </summary>
        ''' <returns></returns>
        Public Property Reference As String
        Public Property Parameters As Expression()
        Public Property [operator] As Boolean

        Public Overrides Function ToSExpression() As String
            If [operator] Then
                Return $"({Reference} {Parameters.Select(Function(a) a.ToSExpression).JoinBy(" ")})"
            Else
                Return $"(call {Reference} {Parameters.Select(Function(a) a.ToSExpression).JoinBy(" ")})"
            End If
        End Function
    End Class

    Public Class LiteralExpression : Inherits Expression

        Public Property type As Type
        Public Property value As String

        Public Overrides Function ToSExpression() As String
            Return $"({Types.Convert2Wasm(type)}.const {value})"
        End Function
    End Class

    Public Class GetLocalVariable : Inherits Expression

        Public Property var As String

        Public Overrides Function ToSExpression() As String
            Return $"(local.get {var})"
        End Function
    End Class

    Public Class SetLocalVariable : Inherits Expression

        Public Property var As String
        Public Property value As Expression

        Public Overrides Function ToSExpression() As String
            Return $"(local.set {var} ({value}))"
        End Function
    End Class

    Public Class GetGlobalVariable : Inherits Expression

        Public Property var As String

        Public Overrides Function ToSExpression() As String
            Return $"(global.get {var})"
        End Function
    End Class

    Public Class SetGlobalVariable : Inherits Expression

        Public Property var As String

        Public Overrides Function ToSExpression() As String
            Return $"(global.set {var})"
        End Function
    End Class

    Public Class DeclareLocal : Inherits Expression

        Public Property name As String
        Public Property type As String
        Public Property init As Expression

        Public Overrides Function ToSExpression() As String
            If init Is Nothing Then
                Return $"(local {name} {type})"
            Else
                Return $"(local {name} {type}) 
{New SetLocalVariable With {.var = name, .value = init}.ToSExpression}"
            End If
        End Function
    End Class

    Public Class Parenthesized : Inherits Expression

        Public Property Internal As Expression

        Public Overrides Function ToSExpression() As String
            Return $"call $ParenthesizedStack {Internal}"
        End Function
    End Class

    Public Class ExportSymbolExpression : Inherits Expression

        ''' <summary>
        ''' 在对象进行导出的时候对外的名称
        ''' </summary>
        ''' <returns></returns>
        Public Property Name As String
        ''' <summary>
        ''' 导出对象的类型，一般为``func``函数类型
        ''' </summary>
        ''' <returns></returns>
        Public Property type As String
        ''' <summary>
        ''' 目标对象在模块内部的引用名称
        ''' </summary>
        ''' <returns></returns>
        Public Property target As String

        Public Overrides Function ToSExpression() As String
            Return $"(export ""{Name}"" ({type} {target}))"
        End Function
    End Class
End Namespace