using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;

Console.WriteLine("Hello, Browser!");
Console.WriteLine("Main thread: " + Thread.CurrentThread.ManagedThreadId);

new Thread(() =>
{
    Console.WriteLine("Background thread: " + Thread.CurrentThread.ManagedThreadId);
}).Start();

