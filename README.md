<p align="center">
    <img src="https://raw.githubusercontent.com/Oxule/Celerio/main/Design/logo_full.png" alt="Celerio Logo"/>
    <img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/Celerio">
    <img alt="NuGet Version" src="https://img.shields.io/nuget/vpre/Celerio">
    <img alt="GitHub last commit" src="https://img.shields.io/github/last-commit/Oxule/Celerio">
</p>


Celerio is a [**Fastest**](BENCHMARKS.MD) Framework for Building HTTP **Web Apps** in **C#**.

* [X1.5 times faster than an ASP.NET](BENCHMARKS.MD)
* Based on a [Roslyn Incremental Source Generators](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)
* Uses no reflexion **at all**

## ⚠️⚠️ **Do not use in production** ⚠️⚠️
> Framework still in alpha as a *proof-of-concept*

## Installation
Just install nuget package `https://www.nuget.org/packages/Celerio`

## Usage

1. Create some `public static` controller class
```csharp
public static class Controller {
    . . .
}
```

2. In the same file add these imports :
```csharp
using Celerio;
using static Celerio.Result;
```
> *The `using static Result` one is optional, but it shortens `Result.Ok()` -> `Ok()`*

3. Create an `public static` endpoint with return type either `Result` or `Task<Result>` and route attribute.

```csharp
[Get("/sum")]
public static Result Sum(int a, int b)
{
    return Ok().Text(a+b);
}
```

4. Then just create server instance and run it

```csharp
var server = new Server(IPAddress.Any, 5000);
server.Start();
await Task.Delay(Timeout.Infinite);
```

## Documentation
See documentation [here](DOCS.md)

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=Oxule/Celerio&type=Date)](https://www.star-history.com/#Oxule/Celerio&Date)

## Contacts
Oxule (Kirill Filonov)

[ribb2017@mail.ru](mailto://ribb2017@mail.ru)

[Telegram](https://t.me/Oxule)