(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)

//Nota sobre IOResult:
//  Si después de los tags {$I-} {$I+}, no se utiliza el valor de IOResult,
//La bandera de error sigue levantada!!!, lo que ocasiona bugs si no se toma en cuenta
unit Usuarios;
interface
uses Objetos;
const
  MAX_ADMINISTRADORES=15;
//permisosUsuario:
  puLimpiarMapas=     $0001;
  puTeletransportar=  $0002;
  puEliminarBolsas=   $0004;
  puCrearObjeto=      $0008;

  puControlarMonstruo=$0010;
  puConjurarMonstruo= $0020;
  puDisolverMonstruo= $0040;

  puBuscarAvatar=     $0100;
  puVisitarAvatar=    $0200;
  puConvocarAvatar=   $0400;
  puEncarcelarAvatar= $0800;
  puRestaurarAvatar=  $1000;
  puExpulsarAvatar=   $2000;
  puAnularClan=      $10000;
  puModerarChat=     $20000;

  cp_Moderador=puModerarChat or puLimpiarMapas or puRestaurarAvatar or
     puBuscarAvatar or puVisitarAvatar or puControlarMonstruo or puTeletransportar or puEncarcelarAvatar;
  cp_Admin=cp_Moderador or puEliminarBolsas or puConjurarMonstruo or puDisolverMonstruo or
    puConvocarAvatar or puExpulsarAvatar or puAnularClan;
  cp_GameMaster=cp_Admin or puCrearObjeto;
  cp_ModoPruebas=puBuscarAvatar or puVisitarAvatar or puTeletransportar or puCrearObjeto;
type
  TErrorAdmUsuarios=(elOk,elPassword,elYaLogueado,elNoExiste,elBaneado,elYaExiste,elDenegado,elDestruido,elEsGameMaster,elNoSePudoGuardar,elLimiteExedido);
  TDatosAdministrador=record
    LoginAdm:TCadenaLogin;
    EstadoUsuario:TEstadoUsuario;
    Nousado1:word;
    Nousado2:integer;
  end;
  TListaAdministradores=array[0..MAX_ADMINISTRADORES] of TDatosAdministrador;
  TArchAdministradores=record
    NroAdministradores:byte;
    NoUsado1:byte;
    NoUsado2:word;
    IdentificadorDeServidor:integer;
    NoUsado4,NoUsado5:integer;
    ListaAdministradores:TListaAdministradores;
  end;



procedure InicializarRegistro(const directorio:string);
function iniciarSesion(const Login:TCadenaLogin;const Password:TSha256Hash;idConexion:integer):TErrorAdmUsuarios;
function crearCuenta(const PasswordInicial:TSha256Hash;const datosNuevoPersonaje:TDatosNuevoPersonaje; var IdentificadorUU:TcadenaLogin;IPCliente:integer):TErrorAdmUsuarios;
function AgregarAdministrador(IdConexion:integer;Estado:TEstadoUsuario):TErrorAdmUsuarios;
function EliminarAdministrador(const cad:string):TErrorAdmUsuarios;
procedure GuardarUsuario(idUsuario:integer);
function LeerArchivoClanes:boolean;
function LeerInfCastillos:boolean;
function GuardarInfCastillos:boolean;
function LeerInfPrecios:boolean;
function GuardarInfPrecios:boolean;
function GuardarClanesJugadores:boolean;
function ObtenerIdConexion(const login:TCadenaLogin):word;
function ObtenerTiempoActualCodWord():integer;

var ArchAdministradores:TArchAdministradores;

implementation
uses smain,globales,sysutils,Mundo,Demonios,tablero;

const
  ARCH_AVATARES_ADM='admin.dat';
  ARCH_CLANES='clanes.dat';
  ARCH_CASTILLOS='castillos.dat';
  ARCH_PRECIOS='precios.dat';

var
  DirRaiz:string;

function ObtenerTiempoActualCodWord():integer;
begin
  result:=trunc(now)-30000;
end;

function EscribirArchivoPrivilegios:boolean;
var fadm:file of TArchAdministradores;
begin
  {$I-}
    assignFile(fadm,DirRaiz+ARCH_AVATARES_ADM);
    Rewrite(fadm);
    write(fadm,ArchAdministradores);
    CloseFile(fadm);
  {$I+}
  result:=IOResult=0;
end;

procedure InicializarRegistro(const directorio:string);
  function LeerPrivilegios:boolean;
  var fadm:file of TArchAdministradores;
  begin
    {$I-}
      assignFile(fadm,DirRaiz+ARCH_AVATARES_ADM);
      Filemode:=0;
      Reset(fadm);
      read(fadm,ArchAdministradores);
      CloseFile(fadm);
    {$I+}
    with ArchAdministradores do
      if NroAdministradores>MAX_ADMINISTRADORES+1 then NroAdministradores:=MAX_ADMINISTRADORES+1;
    result:=IOResult=0;
  end;
begin
  DirRaiz:=directorio;
  if not LeerPrivilegios then
  begin
    fillchar(ArchAdministradores,sizeOf(ArchAdministradores),0);
    ArchAdministradores.NroAdministradores:=0;
    ArchAdministradores.IdentificadorDeServidor:=
      (random($7FFFFF) shl 8) or versionLA;
    EscribirArchivoPrivilegios;
  end;
end;

function LeerArchivoClanes:boolean;
var fclan:file;
    i,tamanno,tammanoLeido:integer;
begin
  //primero asegurarnos que todos los clanes sean destruidos, anulando al lider:
  for i:=0 to maxClanesJugadores do
    ClanJugadores[i].Lider:='';
  {$I-}
    assignFile(fclan,DirRaiz+ARCH_CLANES);
    Filemode:=0;
    Reset(fclan,1);
    tamanno:=TAMANNO_DE_INSTANCIA_DE_TCLANJUGADORES-4;//Tamaño sin referencia a clase.
    while not eof(fclan) do
    begin
      blockRead(fclan,i,4);
      if i>maxClanesJugadores then break;
      //Copiar al objeto destino, no sobreescribir la referencia a su clase.
      BlockRead(fclan,pointer(integer(ClanJugadores[i])+4)^,tamanno,tammanoLeido);
      ClanJugadores[i].MiembrosActivos:=0;
      ClanJugadores[i].codigoClan:=i;
      if tammanoLeido<>tamanno then break;
    end;
    CloseFile(fclan);
  {$I+}
  result:=IOResult=0;
end;

function GuardarClanesJugadores:boolean;
var i,tamanno:integer;
    f:file;
begin
  {$I-}
    assignFile(f,DirRaiz+ARCH_CLANES);
    Rewrite(f,1);
    tamanno:=TAMANNO_DE_INSTANCIA_DE_TCLANJUGADORES-4;//Tamaño sin referencia a clase.
    for i:=0 to maxClanesJugadores do
      if ClanJugadores[i].lider<>'' then
      begin
        BlockWrite(f,i,4);
        //Copiar al objeto destino, no sobreescribir la referencia a su clase.
        BlockWrite(f,pointer(integer(ClanJugadores[i])+4)^,tamanno);
      end;
    CloseFile(f);
  {$I+}
  result:=IOResult=0;
end;

function LeerInfCastillos:boolean;
var i:integer;
    f:file;
begin
  {$I-}
    assignFile(f,DirRaiz+ARCH_CASTILLOS);
    Filemode:=0;
    Reset(f,1);
    i:=0;
    while not eof(f) do
    begin
      if i>maxMapas then break;
      with Mapa[i] do
        BlockRead(f,castillo,sizeof(castillo));
      inc(i);
    end;
    CloseFile(f);
  {$I+}
  result:=IOResult=0;
end;

function LeerInfPrecios:boolean;
var i,j,n:integer;
    f:file;
begin
  {$I-}
    assignFile(f,DirRaiz+ARCH_PRECIOS);
    Filemode:=0;
    Reset(f,1);
    i:=0;
    while not eof(f) do
    begin
      if i>maxMapas then break;
      with Mapa[i] do
      begin
        BlockRead(f,n,4);
        if n>(max_comerciantes+1) then break;//formato incorrecto!!
        for j:=0 to n-1 do
          BlockRead(f,Comerciante[j].inflacion,sizeOf(Comerciante[j].inflacion));
      end;
      inc(i);
    end;
    CloseFile(f);
  {$I+}
  result:=IOResult=0;
end;

function GuardarInfCastillos:boolean;
var i:integer;
    f:file;
begin
  {$I-}
    assignFile(f,DirRaiz+ARCH_CASTILLOS);
    Rewrite(f,1);
    for i:=0 to maxMapas do
    with Mapa[i] do
    begin
      BlockWrite(f,castillo,sizeof(castillo));
    end;
    CloseFile(f);
  {$I+}
  result:=IOResult=0;
end;

function GuardarInfPrecios:boolean;
var i,j,n:integer;
    f:file;
begin
  {$I-}
    assignFile(f,DirRaiz+ARCH_PRECIOS);
    Rewrite(f,1);
    for i:=0 to maxMapas do
    with Mapa[i] do
    begin
      n:=NumeroDeComerciantes;
      if n>(max_comerciantes+1) then n:=(max_comerciantes+1);
      BlockWrite(f,n,4);
      for j:=0 to n-1 do
        BlockWrite(f,Comerciante[j].inflacion,sizeOf(Comerciante[j].inflacion));
    end;
    CloseFile(f);
  {$I+}
  result:=IOResult=0;
end;

function VerificarPrivilegios(IdConexion:integer):integer;
//-1=no existe en la lista, 0=primera posicion
// en la var. estadoUsuario devuelve el estado si está en la lista.
var i:integer;
begin
  with ArchAdministradores do
    for i:=0 to NroAdministradores-1 do
      if ListaAdministradores[i].LoginAdm=DatosUsuario[idConexion].IdLogin then
      begin
        result:=i;
        with DatosUsuario[idConexion] do
        begin
          estadoUsuario:=ListaAdministradores[i].EstadoUsuario;
          case estadoUsuario of
            euModerador:begin
              PermisosDelUsuario:=cp_Moderador;
              Jugador[idConexion].comportamiento:=comModerador;
            end;
            euAdmin:begin
              PermisosDelUsuario:=cp_Admin;
              Jugador[idConexion].comportamiento:=comAdmin;
            end;
            euGameMaster:begin
              PermisosDelUsuario:=cp_GameMaster;
              Jugador[idConexion].comportamiento:=comGameMaster;
            end;
            else break;
          end;
        end;
        exit;
      end;
  DatosUsuario[idConexion].estadoUsuario:=euNormal;
  DatosUsuario[idConexion].PermisosDelUsuario:=$0;
  Jugador[idConexion].comportamiento:=comNormal;
  result:=-1;
end;

function AgregarAdministrador(IdConexion:integer;Estado:TEstadoUsuario):TErrorAdmUsuarios;
var indiceAdm:integer;
  procedure ActualizarEstado;
  begin
    with Jugador[idConexion] do
    case estado of
      euGameMaster:begin
        DatosUsuario[idConexion].PermisosDelUsuario:=cp_GameMaster;
        cambiarHonor(Jugador[idConexion],comGameMaster);
      end;
      euAdmin:begin
        DatosUsuario[idConexion].PermisosDelUsuario:=cp_Admin;
        cambiarHonor(Jugador[idConexion],comAdmin);
      end;
      euModerador:begin
        DatosUsuario[idConexion].PermisosDelUsuario:=cp_Moderador;
        cambiarHonor(Jugador[idConexion],comModerador);
      end;
    end;
  end;
begin
  result:=elOK;
  indiceAdm:=VerificarPrivilegios(IdConexion);
  with ArchAdministradores do
    if indiceAdm>=0 then//Ya es administrador
    begin
      ListaAdministradores[indiceAdm].EstadoUsuario:=Estado;
      DatosUsuario[idConexion].EstadoUsuario:=Estado;
      ActualizarEstado;
    end
    else//NO era admin
      if NroAdministradores<=MAX_ADMINISTRADORES then
      begin
        ListaAdministradores[NroAdministradores].LoginAdm:=DatosUsuario[idConexion].IdLogin;
        ListaAdministradores[NroAdministradores].EstadoUsuario:=Estado;
        DatosUsuario[idConexion].EstadoUsuario:=Estado;
        ActualizarEstado;
        inc(NroAdministradores);
      end
      else
        result:=elLimiteExedido;
  if result=elOk then
    if not EscribirArchivoPrivilegios then
      result:=elNoSePudoGuardar;
end;

function EliminarAdministrador(const cad:string):TErrorAdmUsuarios;
var indiceAdm,ID_Conexion:integer;
begin
  result:=elNoExiste;
  with ArchAdministradores do
    for indiceAdm:=0 to NroAdministradores-1 do
      if ListaAdministradores[indiceAdm].LoginAdm=cad then
      begin
        dec(NroAdministradores);
        //colocar el último en la posicion eliminada
        ListaAdministradores[indiceAdm]:=ListaAdministradores[NroAdministradores];
        if EscribirArchivoPrivilegios then
        begin
          //Para obtener efecto inmediato:
          ID_Conexion:=ObtenerIdConexion(cad);
          if ID_Conexion<=MaxJugadores then//logueado
            with Jugador[id_Conexion] do
            begin
              DatosUsuario[ID_Conexion].EstadoUsuario:=euNormal;
              //Informar del nuevo estado
              sendText(id_conexion,'I'+#19+char(euNormal));
              CambiarHonor(Jugador[id_Conexion],comNormal);
            end;
          result:=elOk
        end
        else
          result:=elNoSePudoGuardar;
        exit;//no continuar revisando más avatares
      end;
end;

function LeerCuenta(const identificador:TCadenaLogin;idConexionUsuario:integer;var ElJugador:TJugadorS):TErrorAdmUsuarios;
var fusu:file of Tusuario;
    usuario:TUsuario;
    NombreArchivo:string;
begin
  result:=elNoExiste;
  nombreArchivo:=DirRaiz+identificador+EXT_ARCH_AVATARES;
{$I-}
  assignFile(fusu,nombreArchivo);
  Filemode:=0;
  Reset(fusu);
  read(fusu,usuario);
  CloseFile(fusu);
{$I+}
  if (IOResult=0) then//siempre revisar IOResult después de {$I-}
    if VERSION_AVATAR=Usuario.Datos.version then
    begin
      //Copiar al objeto destino, no sobreescribir la referencia a su clase.
      if TAMANNO_DE_INSTANCIA_DE_TJUGADORS = TJugadorS.InstanceSize then
        move(Usuario.Personaje,pointer(integer(ElJugador)+4)^,TAMANNO_DE_INSTANCIA_DE_TJUGADORS-4);
      DatosUsuario[idConexionUsuario]:=Usuario.datos;
      with DatosUsuario[idConexionUsuario] do
      begin//calculados:
        ultimoIngreso:=ObtenerTiempoActualCodWord;
        UltimoIP:=SocketDelJugador[idConexionUsuario].RemoteAddr.sin_addr.S_addr;
        if (EstadoUsuario>euNormal) then
          VerificarPrivilegios(idConexionUsuario);
        if ServidorEnModoDeVerificacion then
          PermisosDelUsuario:=PermisosDelUsuario or cp_ModoPruebas;
        if EstadoUsuario<=euBaneado then
          result:=elBaneado
        else
          result:=elOk;
      end;
    end
    else
      result:=elDestruido
end;

function crearCuenta(const PasswordInicial:TSha256Hash;const datosNuevoPersonaje:TDatosNuevoPersonaje; var IdentificadorUU:TcadenaLogin;IPCliente:integer):TErrorAdmUsuarios;
var JugadorNuevo:TjugadorS;
    usuario:Tusuario;
begin
  result:=elYaExiste;
  IdentificadorUU:=ObtenerLoginDeCadena(datosNuevoPersonaje.nombre);
  if fileexists(DirRaiz+IdentificadorUU+EXT_ARCH_AVATARES) then exit;
  JugadorNuevo:=TjugadorS.create(0);
  if JugadorNuevo.nuevoPersonaje(datosNuevoPersonaje) then
  begin
    with Usuario.datos do
    begin
      version:=VERSION_AVATAR;
      EstadoUsuario:=euNormal;
      DiaDeCreacion:=ObtenerTiempoActualCodWord;
      PermisosDelUsuario:=$0;
      ultimoIP:=IPCliente;
      IdLogin:=IdentificadorUU;
      ultimoIngreso:=DiaDeCreacion;
      AgresividadVerbal:=0;
      Password:=PasswordInicial;
    end;
    if EscribirCuenta(Usuario.datos,dirRaiz,JugadorNuevo) then
      result:=elOk
    else
      result:=elDenegado;
  end
  else
    result:=elDenegado;
  JugadorNuevo.free;
end;

function iniciarSesion(const Login:TCadenaLogin;const Password:TSha256Hash;idConexion:integer):TErrorAdmUsuarios;
//Recupera la información del jugador, preparándolo para ingresar a un mapa.
begin
  if Login<>ObtenerLoginDeCadena(Login) then//evita abrir archivos con nombres invalidos
    result:=elNoExiste
  else
    if ObtenerIdConexion(Login)<=MaxJugadores then//logueado
      result:=elYaLogueado
    else
    begin
      result:=LeerCuenta(Login,idConexion,Jugador[idConexion]);
      if result=elOk then
        if PasswordsIguales(Password,DatosUsuario[idConexion].password) then
          result:=elOk
        else
          result:=elPassword;
    end;
  if result<>elOk then
  begin
    DatosUsuario[idConexion].estadoUsuario:=euNoConectado;//cerrarSesion
    Jugador[idConexion].activo:=false;//Para evitar enviarle datos.
  end;
end;

procedure GuardarUsuario(idUsuario:integer);
begin
  if not ServidorEnModoDeVerificacion then
    if DatosUsuario[idUsuario].IdLogin<>'' then
      EscribirCuenta(DatosUsuario[idUsuario],dirRaiz,Jugador[idUsuario]);
end;

function ObtenerIdConexion(const login:TCadenaLogin):word;
var i:integer;
begin
  result:=$FFFF;//No conectado
  if length(login)<4 then exit;
  for i:=0 to MaxJugadores do
    if DatosUsuario[i].IdLogin=login then
    begin
      result:=i;
      exit;
    end;
end;

end.



