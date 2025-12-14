# RigbySDK C#

C# SDK для Rigby API. Методы и группы повторяют TypeScript SDK (`@rigbyhost/sdk-ts`): `gdps.*`, `notifications.*`, `user.*` и вложенные `player.songs`, `gdps.server`, и т.д.

## Установка (локально)

```bash
dotnet add package RigbySDK --version 0.1.0 # после публикации на NuGet
# или локально:
dotnet add reference path/to/rigbysdk-csharp/RigbySDK.csproj
```

## Пример

```csharp
using System;
using System.Threading.Tasks;
using RigbySDK;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        var sdk = new RigbyClient("YOUR_API_TOKEN");

        JsonElement cfg = await sdk.GDPS.Config.Get(new { srvId = "my-server-id" });
        Console.WriteLine(cfg);

        JsonElement levels = await sdk.GDPS.Levels.Search(new { srvId = "my-server-id", query = "demon" });
        Console.WriteLine(levels);

        JsonElement me = await sdk.User.Me();
        Console.WriteLine(me);
    }
}
```

## Ошибки

HTTP ошибки выбрасывают `RigbySDKException` (поля `StatusCode`, `Body`). Ошибки сериализации/сети — стандартные исключения .NET.

## Конфигурация

```csharp
var sdk = new RigbyClient("TOKEN", options => {
    options.BaseUrl = "https://api.rigby.host";
    options.HttpClientTimeout = TimeSpan.FromSeconds(30);
});
```
