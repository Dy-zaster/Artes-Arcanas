(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)

//Módulo libre de bibliotecas externas al juego
unit Globales;
interface
uses Objetos,demonios;
const
//Generales
  PUERTO_COMUNICACION=31715;
  M_FaltanArchivosDelJuego='Faltan archivos del juego';
  M_EjecutableDannado='¡Ejecutable Dañado!';
//  CaracteresNoPermitidos:set of char=[#0..#31,',','.','\','/',':','*','?','"','<','>','|','(',')',#127];
  CaracteresPermitidos:set of char=['a'..'z','A'..'Z','0'..'9',#32,'-','_','''',#159,#192..#214,#216..#246,#248..#255];
  MIN_PUERTO_COMUNICACION=21;
  MAX_PUERTO_COMUNICACION=32767;
  //Opciones del servidor, Flags (4)
  FS_ModoDePruebas=$80;
  FS_ComunicacionTotal=$40;
  CARPETA_AVATARES='avatares\';
  EXT_ARCH_AVATARES='.avt';
  VERSION_AVATAR=0;
  //Siempre tiene que ser igual al tamaño usado en el servidor para TJugadorS
  TAMANNO_DE_INSTANCIA_DE_TJUGADORS=296;
  //Siempre revisar que sea el correcto al cambiar TClanJugadores en el servidor
  TAMANNO_DE_INSTANCIA_DE_TCLANJUGADORES=64;

type
  TPersonaje=array[0..TAMANNO_DE_INSTANCIA_DE_TJUGADORS-5] of byte;
  TUsuario=record
    Datos:TDatosUsuario;
    Personaje:TPersonaje;
  end;

  function PareceIP(const cadena:string):boolean;
  function EmpaquetarPassword(const cad,identificador:string):TSha256Hash;
  function PasswordAStr(const Login:TSha256Hash):TCadena39;
  function ObtenerLoginDeCadena(const cadena:string):TCadenaLogin;
  function QuitarCaracteresNoPermitidos(const cadena:string):string;
//de cadena encriptada a TPassword y viceversa
  function ControlParametroEntero(const cadena:string;LimInf,LimSup:integer;var numero:integer):boolean;
  function EscribirCuenta(const DatosUsuario:TDatosUsuario;const Carpeta:string;Jugador:TjugadorS):boolean;
  function IPAutorizado(const IP:integer):boolean;
  function ObtenerHandshake(NroAleatorioServidor:cardinal):cardinal;
  function HandshakeCorrecto(Codigo_Autenticacion,NroAleatorioServidor:cardinal):boolean;

  function PasswordsIguales(const password1,password2:TSha256Hash):boolean;  
  function calc_sha_256(input:PByte; len:cardinal):TSha256Hash;

var
  ID_ATOM_SOLO_UNA_INSTANCIA:word;
  Ruta_Aplicacion:string;

implementation

function PasswordsIguales(const password1,password2:TSha256Hash):boolean;
var i:integer;
begin
  result:=false;
  for i:=0 to 31 do
    if password1[i]<>password2[i] then exit;
  result:=true;
end;

function ControlParametroEntero(const cadena:string;LimInf,LimSup:integer;var numero:integer):boolean;
var code:integer;
begin
  val(cadena,numero,code);
  result:=(code=0) and (Numero>=LimInf) and (Numero<=LimSup);
end;

function ObtenerLoginDeCadena(const cadena:string):TCadenaLogin;
var i,longitud:integer;
    ncar:char;
    quitarEspacios:boolean;
begin
  result:='';
  longitud:=length(cadena);
  //filtrar caracteres
  quitarEspacios:=true;
  for i:=1 to longitud do
  begin
    if quitarEspacios and (cadena[i] = ' ') then continue;
    quitarEspacios:=false;

    if (cadena[i] in caracteresPermitidos) then
    begin
      case cadena[i] of
        'a'..'z':ncar:=upcase(cadena[i]);
        'A'..'Z','0'..'9':ncar:=cadena[i];
        'Á','À','Ä','Â','á','à','ä','â','Å','å','Ã','ã':ncar:='A';
        'É','È','Ë','Ê','é','è','ë','ê':ncar:='E';
        'Í','Ì','Ï','Î','í','ì','ï','î':ncar:='I';
        'Ó','Ò','Ö','Ô','ó','ò','ö','ô','Õ','õ':ncar:='O';
        'Ú','Ù','Ü','Û','ú','ù','ü','û':ncar:='U';
        'Ý','ý','ÿ','Ÿ':ncar:='Y';
        'ñ','Ñ':ncar:='N';
        'ç','Ç':ncar:='C';
        else
          ncar:='_';
      end;
      result:=result+ncar;
      if length(result)=16 then exit;//se llegó al máximo
    end;
  end;

  //evitar nombres problema como aux, com1, com2, prn, lpt1, etc.
  longitud:=length(result);
  while (longitud<5) do
  begin
    result:= result + '_';
    longitud:=length(result);
  end;
end;

function QuitarCaracteresNoPermitidos(const cadena:string):string;
var i:integer;
begin
  result:='';
  for i:=1 to length(cadena) do
    if (cadena[i] in caracteresPermitidos) then result:=result+cadena[i];
end;

function PasswordAStr(const Login:TSha256Hash):TCadena39;
var i:integer;
begin
  result:='';
  for i:=0 to 31 do result:=result+char(login[i]);
end;

function EmpaquetarPassword(const cad,Identificador:string):TSha256Hash;
var temp:TCadena255;
begin
  temp:='Artes Arcanas:'+identificador+cad;
  result:=calc_sha_256(@temp,cardinal(temp[0])+1);
end;

function IPAutorizado(const IP:integer):boolean;
//IMPLEMENTAR AQUI EL CONTROL DE RESTRICCIONES DE IP.
begin
  result:=true;
end;

function ObtenerHandshake(NroAleatorioServidor:cardinal):cardinal;
const TOKEN_HAND_SHAKE=$542C3A9E;
  function XRandomSeed(seed:cardinal):cardinal; register;
  // seed en EAX, result y seed en EAX
  asm
    mov edx, eax
    shl eax, 13
    xor edx, eax

    mov eax, edx
    shr eax, 17
    xor edx, eax

    mov eax, edx
    shl eax, 5
    xor eax, edx
  end;
begin
  result:=XRandomSeed(NroAleatorioServidor +
            XRandomSeed(TOKEN_HAND_SHAKE xor NroAleatorioServidor));
end;

function HandshakeCorrecto(Codigo_Autenticacion,NroAleatorioServidor:cardinal):boolean;
begin
  result:=ObtenerHandshake(NroAleatorioServidor)=Codigo_Autenticacion;
end;

function EscribirCuenta(const DatosUsuario:TDatosUsuario;const Carpeta:string;Jugador:TjugadorS):boolean;
var fusuae:file of Tusuario;
    usuario:Tusuario;
    NombreArchivo:string;
    DeshabilitarArchivoOriginal:boolean;
begin
  usuario.datos:=DatosUsuario;
  DeshabilitarArchivoOriginal:=usuario.datos.EstadoUsuario=euBaneado;
  if DeshabilitarArchivoOriginal then
    usuario.datos.EstadoUsuario:=euNormal;
//Parte peligrosa:
//----------------------------------------
  //Copiar el objeto (menos referencia a clase) a la variable destino (evitando pisar los 4 bytes iniciales de referencia a clase)
  if TAMANNO_DE_INSTANCIA_DE_TJUGADORS = TJugadorS.InstanceSize then
    move(pointer(integer(jugador)+4)^,Usuario.Personaje,TAMANNO_DE_INSTANCIA_DE_TJUGADORS-4);
//---------------------------------------------
  nombreArchivo:=Carpeta+Usuario.datos.IdLogin+EXT_ARCH_AVATARES;
  if DeshabilitarArchivoOriginal then
  begin
    {$I-}
    assignFile(fusuae,nombreArchivo);
    erase(fusuae);//Sólo si se abre el archivo cerrarlo antes de borrarlo
    {$I+}
    result:=IOResult=0;
    nombreArchivo:=nombreArchivo+'.ban';//grabar el nuevo con distinto nombre
  end
  else
    result:=true;

  assignFile(fusuae,nombreArchivo);
  {$I-}
  Rewrite(fusuae);
  write(fusuae,usuario);
  CloseFile(fusuae);
  {$I+}
  result:=result and (IOResult=0);
end;

function PareceIP(const cadena:string):boolean;
const LOS_CARACTERES_DE_UN_IP: set of char=['0'..'9','.'];

var i,contaPuntos:integer;
begin
  result:=false;
  if (length(cadena)<7) or (length(cadena)>15) then exit;
  if not (cadena[1] in ['0'..'9']) then exit;
  contaPuntos:=0;
  for i:=2 to length(cadena) do
  begin
    if not (cadena[i] in LOS_CARACTERES_DE_UN_IP) then exit;
    if cadena[i]='.' then inc(contaPuntos);
  end;
  if contapuntos<>3 then exit;
  result:=true;
end;

function calc_sha_256(input:PByte; len:cardinal):TSha256Hash;
const
  CHUNK_SIZE=64;
  TOTAL_LEN_LEN=64;

(*
 * Initialize array of round constants:
 * (first 32 bits of the fractional parts of the cube roots of the first 64 primes 2..311):
 *)

k:array[0..63] of cardinal= (
	$428a2f98, $71374491, $b5c0fbcf, $e9b5dba5, $3956c25b, $59f111f1, $923f82a4, $ab1c5ed5,
	$d807aa98, $12835b01, $243185be, $550c7dc3, $72be5d74, $80deb1fe, $9bdc06a7, $c19bf174,
	$e49b69c1, $efbe4786, $0fc19dc6, $240ca1cc, $2de92c6f, $4a7484aa, $5cb0a9dc, $76f988da,
	$983e5152, $a831c66d, $b00327c8, $bf597fc7, $c6e00bf3, $d5a79147, $06ca6351, $14292967,
	$27b70a85, $2e1b2138, $4d2c6dfc, $53380d13, $650a7354, $766a0abb, $81c2c92e, $92722c85,
	$a2bfe8a1, $a81a664b, $c24b8b70, $c76c51a3, $d192e819, $d6990624, $f40e3585, $106aa070,
	$19a4c116, $1e376c08, $2748774c, $34b0bcb5, $391c0cb3, $4ed8aa4a, $5b9cca4f, $682e6ff3,
	$748f82ee, $78a5636f, $84c87814, $8cc70208, $90befffa, $a4506ceb, $bef9a3f7, $c67178f2
);

type TBufferState=record
  p:PByte;
	 len:cardinal;
	 total_len:cardinal;
	 single_one_delivered:boolean;
	 total_len_delivered:boolean;
end;
type PBufferState=^TBufferState;
type TChunk=array[0..CHUNK_SIZE-1] of byte;

function right_rot(value:cardinal; count:byte): cardinal; assembler;
asm
  mov cl, count
  rol eax, cl
end;

procedure init_buf_state(var state:TBufferState; input:PByte; len:cardinal);
begin
	 state.p := input;
	 state.len := len;
	 state.total_len := len;
	 state.single_one_delivered := false;
	 state.total_len_delivered := false;
end;

function calc_chunk(var chunk:TChunk; var state:TBufferState):boolean;
var
  space_in_chunk:cardinal;
  left,len:cardinal;
  i:integer;
  pChunk:PByte;
begin
  if (state.total_len_delivered) then
  begin
    result:=false;
    exit;
  end;

  if (state.len >= CHUNK_SIZE) then
  begin
    move(state.p^, chunk, CHUNK_SIZE);
    inc(state.p, CHUNK_SIZE);
    dec(state.len, CHUNK_SIZE);
    result:=true;
    exit;
  end;

  move(state.p^, chunk, state.len);
  pChunk:=@chunk;
  inc(pChunk, state.len);
  space_in_chunk := CHUNK_SIZE - state.len;
  inc(state.p, state.len);
  state.len := 0;

  // If we are here, space_in_chunk is one at minimum.
  if (not state.single_one_delivered) then
  begin
    pChunk^ := $80;
    inc(pChunk);
    dec(space_in_chunk);
    state.single_one_delivered := true;
  end;

  (*
  * Now:
  * - either there is enough space left for the total length, and we can conclude,
  * - or there is too little space left, and we have to pad the rest of this chunk with zeroes.
  * In the latter case, we will conclude at the next invokation of this function.
  *)
  if (space_in_chunk >= TOTAL_LEN_LEN) then
  begin
    left := space_in_chunk - TOTAL_LEN_LEN;
    len := state.total_len;
    fillchar(pChunk^, left, 0);
    inc(pChunk, left);

    // Storing of len * 8 as a big endian 64-bit without overflow.
    PByte(cardinal(pChunk)+7)^ := byte(len shl 3);
    len := len shr 5;
    for i:=6 downto 0 do
    begin
      PByte(cardinal(pChunk)+cardinal(i))^ := byte(len);
      len := len shr 8;
    end;

    state.total_len_delivered := true;
  end
  else
  begin
    fillchar(pChunk^, space_in_chunk, 0);
  end;

  result:=true;
end;

var
  h:array[0..7] of cardinal;
  ah:array[0..7] of cardinal;
  w:array[0..15] of cardinal;
  i,j,d,s0,s1,ch,temp1,temp2,maj:cardinal;
  chunk:TChunk;
  state:TBufferState;
  p:PByte;
begin
	(*
	 * Initialize hash values:
	 * (first 32 bits of the fractional parts of the square roots of the first 8 primes 2..19):
	 *)
  h[0] := $6a09e667;
  h[1] := $bb67ae85;
  h[2] := $3c6ef372;
  h[3] := $a54ff53a;
  h[4] := $510e527f;
  h[5] := $9b05688c;
  h[6] := $1f83d9ab;
  h[7] := $5be0cd19;
  init_buf_state(state, input, len);

	 while calc_chunk(chunk, state) do
  begin
 	 p := @chunk;
 	 // Initialize working variables to current hash value:
 	 for i := 0 to 7 do ah[i] := h[i];

	 	 // Compression function main loop:
	   for i := 0 to 3 do
    begin
			(*
			 * The w-array is really w[64], but since we only need
			 * 16 of them at a time, we save stack by calculating
			 * 16 at a time.
			 *
			 * This optimization was not there initially and the
			 * rest of the comments about w[64] are kept in their
			 * initial state.
			 *)

			(*
			 * create a 64-entry message schedule array w[0..63] of 32-bit words
			 * (The initial values in w[0..63] don't matter, so many implementations zero them here)
			 * copy chunk into first 16 words w[0..15] of the message schedule array
			 *)

			 for j := 0 to 15 do
      begin
			   if (i = 0) then
        begin
          d:=(cardinal(p^) shl 24);
          inc(p);
          d:=d or (cardinal(p^) shl 16);
          inc(p);
          d:=d or (cardinal(p^) shl 8);
          inc(p);
          d:=d or cardinal(p^);
          inc(p);
				  	w[j] := d;
			   end
        else
        begin
					 // Extend the first 16 words into the remaining 48 words w[16..63] of the message schedule array:
				   s0 := right_rot(w[(j + 1) and $f], 7) xor right_rot(w[(j + 1) and $f], 18) xor (w[(j + 1) and $f] shr 3);
				   s1 := right_rot(w[(j + 14) and $f], 17) xor right_rot(w[(j + 14) and $f], 19) xor (w[(j + 14) and $f] shr 10);
				   w[j] := w[j] + s0 + w[(j + 9) and $f] + s1;
				 end;
				 s1 := right_rot(ah[4], 6) xor right_rot(ah[4], 11) xor right_rot(ah[4], 25);
				 ch := (ah[4] and ah[5]) xor (not ah[4] and ah[6]);
				 temp1 := ah[7] + s1 + ch + k[i shl 4 or j] + w[j];
				 s0 := right_rot(ah[0], 2) xor right_rot(ah[0], 13) xor right_rot(ah[0], 22);
				 maj := (ah[0] and ah[1]) xor (ah[0] and ah[2]) xor (ah[1] and ah[2]);
				 temp2 := s0 + maj;

				 ah[7] := ah[6];
				 ah[6] := ah[5];
				 ah[5] := ah[4];
				 ah[4] := ah[3] + temp1;
				 ah[3] := ah[2];
				 ah[2] := ah[1];
				 ah[1] := ah[0];
				 ah[0] := temp1 + temp2;
			 end
		 end;

	   // Add the compressed chunk to the current hash value:
	   for i := 0 to 7 do
      inc(h[i], ah[i]);
	 end;

	 // Produce the final hash value (big-endian)
  j := 0;
	 for i := 0 to 7 do
  begin
    result[j] := byte(h[i] shr 24);
    inc(j);
    result[j] := byte(h[i] shr 16);
    inc(j);
    result[j] := byte(h[i] shr 8);
    inc(j);
    result[j] := byte(h[i]);
    inc(j);
  end;
end;


end.

