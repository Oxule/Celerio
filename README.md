# Celerio
Celerio is a **Lightweight** and **Fast** Framework for Building HTTP **Web Apps** in **C#**.

## Installation
Just install nuget package `https://www.nuget.org/packages/Celerio`

## Usage
1. At any point in your application, create pipeline instance

```csharp
    var pipeline = new Pipeline();
```

2. If you're going to use authentication, then change crypto keys

```csharp
    pipeline.Authentification = new DefaultAuthentification("key", "salt");
```

3. Configure pipeline any way you want (e.g. Change authentification scheme or add IP blacklist)

4. Create anywhere an endpoint for e.g.

```csharp
    [Route("GET", "/sum", "/add", "/add/{a}/{b}", "/sum/{a}/{b}")]
    public static HttpResponse Sum(int a, int b)
    {
        return HttpResponse.Ok((a+b).ToString());
    }
```

5. Then just create server instance and run it

```csharp
    Server server = new Server(pipeline);
    await server.StartListening(5000);
```

## Documentation
See documentation [here](DOCS.md)

## Contacts
Oxule

`ribb2017@mail.ru`

`@Oxule`