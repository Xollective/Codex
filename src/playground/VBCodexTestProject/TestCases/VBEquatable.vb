Imports System

Public Class VBEquatable
    Implements IEquatable(Of VBEquatable)
    Public Function Equals(other As VBEquatable) As Boolean Implements IEquatable(Of VBEquatable).Equals
        Return ReferenceEquals(Me, other)
    End Function
End Class