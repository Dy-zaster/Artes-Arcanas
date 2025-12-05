(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)
unit buscar_ip;

interface
uses ScktComp,classes;

type
  TGetIPClientSocket=class(TClientSocket)
  private
    fServidor:string;
    fPuerto:integer;
  public
    property Servidor:string read fServidor;
    property Puerto:integer read fPuerto;
    function SolicitarServidor(nombreServidorWEB:string):boolean;
    procedure AlConectar(Sender:TObject;Socket:TCustomWinSocket);
    procedure ObtenerServidorYPuerto(ElSocket:TCustomWinSocket);
  end;

implementation
uses sysutils,juego;

function TGetIPClientSocket.SolicitarServidor(nombreServidorWEB:string):boolean;
begin
  result:=false;
  if nombreServidorWEB='' then exit;
  OnConnect:=AlConectar;
  if Active then Close;
  fservidor:='';
  fpuerto:=0;
  ClientType:=ctNonBlocking;
  Port:=80;
  Host:=nombreServidorWEB;
  Active:=true;
  result:=true;
end;

procedure TGetIPClientSocket.AlConectar(Sender:TObject;Socket:TCustomWinSocket);
begin
  Socket.SendText('GET http://'+host+'/inf.txt'+#13+#10+'Host: '+host+#13+#10+
    'User-Agent: Mozilla/5.0 '+#13+#10+
    'Accept: text/html;q=0.9,text/plain;q=0.8'+#13+#10+
    'Accept-Charset: ISO-8859-1,utf-8'+#13+#10+
    'Keep-Alive: 300'+#13+#10+
    'Connection: keep-alive'+#13+#10+#13+#10);
end;

procedure TGetIPClientSocket.ObtenerServidorYPuerto(ElSocket:TCustomWinSocket);
var posicionSeparador,posicionEspacio,code:integer;
begin
  fservidor:=trim(ElSocket.ReceiveText);
  if copy(fservidor,1,4)<>'LAA=' then exit;
  Delete(fservidor,1,4);
  posicionSeparador:=pos(':',fservidor);
  posicionEspacio:=pos(' ',fservidor);
  Val(copy(fservidor,posicionSeparador+1,posicionEspacio-posicionSeparador-1),fpuerto,code);
  if code<>0 then fpuerto:=0;
  G_GameHosting:=trim(copy(fservidor,posicionEspacio+1,255));
  Delete(fservidor,posicionSeparador,255);
  fservidor:=trim(fservidor);
end;

end.
