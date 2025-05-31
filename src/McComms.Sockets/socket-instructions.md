- El projecte es la implementacio dels interfaces ICommsClient i ICommsServer definits a McComms.Core.

- El problema que vull solventar es un problema de concurrencia entre l'enviament de CommandRequest que genera un BroadcastMessage, si a la gestio del BroadcastMessage envia un nou CommandRequest es bloqueja.

- El doble canal s'ha d'implementar a les clases de nivell superior CommsClientSockets i CommsServerSockets.

- Les clases que implementen la gestio dels sockets, SocketsClient i SocketsServer s'han de deixar com estan.

- Les clases CommsServerSockets i CommsClientsSockets han d'implementar correctament els interfases.

- El port del segon canal, sera el mateix que el principal + 1.