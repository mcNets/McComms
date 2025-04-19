# McComms - Flexible Communications Library

McComms és una biblioteca de comunicacions flexible que proporciona implementacions de client i servidor utilitzant diferents tecnologies de comunicació.

## Visió general

McComms està dissenyat per oferir una interfície comú per a comunicacions client-servidor, permetent diferents implementacions subjacents. Actualment, es suporten les següents tecnologies:

- **Sockets TCP/IP** - Implementació de comunicacions basada en sockets
- **gRPC** - Implementació de comunicacions basada en gRPC

La biblioteca està estructurada per permetre una fàcil substitució de la tecnologia de comunicació sense canviar el codi de l'aplicació principal.

## Estructura del projecte

El projecte està organitzat en els següents mòduls:

- **McComms.Core** - Defineix les interfícies i classes bases comunes
- **McComms.Sockets** - Implementació basada en sockets TCP/IP
- **McComms.gRPC** - Implementació basada en gRPC
- **McComms.Core.Tests** - Tests unitaris per al nucli de la biblioteca

## Característiques principals

- Interfície unificada per a diferents tecnologies de comunicació
- Suport per a operacions síncrones i asíncrones
- Gestió de comunicacions client-servidor
- Suport per a missatges de broadcast
- Codificació i descodificació de missatges

## Instal·lació

El projecte està disponible com a paquets NuGet:

```
Install-Package McComms.Core
Install-Package McComms.Sockets
Install-Package McComms.gRPC
```

## Ús bàsic

### Client

```csharp
// Crear un client utilitzant sockets
var client = new CommsClientSockets();

// Connectar amb el servidor
client.Connect(message => {
    Console.WriteLine($"Broadcast rebut: {message}");
});

// Enviar una comanda i rebre resposta
var response = client.SendCommand(new CommandRequest("COMANDA", "paràmetres"));

// O utilitzant async/await
var response = await client.SendCommandAsync(new CommandRequest("COMANDA", "paràmetres"));

// Desconnectar
client.Disconnect();
```

### Servidor

```csharp
// Crear un servidor utilitzant sockets
var server = new CommsServerSockets();

// Registrar gestors de comandes
server.RegisterCommandHandler("COMANDA", (request) => {
    return new CommandResponse("OK");
});

// Iniciar el servidor
server.Start();

// Enviar un missatge de broadcast a tots els clients
server.Broadcast(new BroadcastMessage("INFO", "Missatge per tots els clients"));

// Aturar el servidor
server.Stop();
```

## Suport per operacions síncrones i asíncrones

Totes les operacions principals admeten tant versions síncrones com asíncrones, permetent-te escollir l'enfocament que millor s'adapti als teus requisits:

```csharp
// Versió síncrona
var response = client.SendCommand(new CommandRequest("COMANDA", "dades"));

// Versió asíncrona
var response = await client.SendCommandAsync(new CommandRequest("COMANDA", "dades"));
```

## Contribució

Les contribucions són benvingudes! Si desitges contribuir a McComms, sisplau:

1. Fes un fork del repositori
2. Crea una branca per la teva característica (`git checkout -b caracteristica/nova-funcionalitat`)
3. Fes commit dels teus canvis (`git commit -am 'Afegir nova funcionalitat'`)
4. Fes push a la branca (`git push origin caracteristica/nova-funcionalitat`)
5. Crea un Pull Request

## Llicència

Aquest projecte està llicenciat sota la llicència MIT. Consulta el fitxer LICENSE per més detalls.
