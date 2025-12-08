(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)

unit Graficador;
interface
uses DirectDraw,Windows,Classes,Graphics,objetos;

const
  SCREEN_W=1280; // 640
  SCREEN_W_D2=SCREEN_W div 2; // 640
  SCREEN_H=720; // 480
  SCREEN_PF=pf16bit;//pf16bit, pf32bit

  ancho_dd=SCREEN_W;
  mitad_ancho_dd=ancho_dd shr 1;
  alto_dd=SCREEN_H - 128;
  mitad_alto_dd=alto_dd shr 1;
  ancho_tile=24;
  alto_tile=16;
  ancho_tile_x2=ancho_tile shl 1;
  alto_tile_x2=alto_tile shl 1;
  DDraw_mitad_Sprite_X=640;//324;
  DDraw_mitad_Sprite_Y=296;//184;
  Rectangulo_Origen:Trect=(left:0;top:0;right:ancho_dd;bottom:alto_dd);
  Area_Dibujable_Zoom:Trect=(left:ancho_dd shr 2;top:alto_dd shr 2;
                            right:(ancho_dd shr 2)*3;bottom:(alto_dd shr 2)*3);
  //Tablas de construccion/comercio:
  ANCHO_TABLA_DER=160;
  ALTO_TABLA_DER=SCREEN_H-128;
  Area_Dibujable_TablaD:Trect=(left:0;top:0;right:ancho_dd-ANCHO_TABLA_DER;bottom:alto_dd);
  Area_Dibujable_TablaD_Zoom:Trect=(left:ancho_dd shr 2;top:alto_dd shr 2;right:ancho_dd-ANCHO_TABLA_DER;bottom:(alto_dd shr 2)*3);
  Msk_FX_Reflejo=$80;
  Msk_FX_Estilo=$7F;
{$DEFINE NO_CONTROLAR_GBM}
     //Archivos graficos
     ExtArcG='.grf';
{$IFDEF NO_CONTROLAR_GBM}
     ExtArc='.bmp';
{$ELSE}
     ExtArc=ExtArcG;
{$ENDIF}
     ExtArc2='.jpg';
     CrptGDD='Grf\';

type
   Tlinea16bits=array[0..8191] of word;
   Tlinea8bits=array[0..8191] of byte;
   Plinea8bits=^Tlinea8bits;
   TImagen8bits=array[0..8191] of Plinea8bits;
   PImagen8bits=^TImagen8bits;
   TFxNocturno=(FxNocheAzul,FXInfravision);
   TFxAlpha=(FxNinguno,FxTrans50,FxPlano,FxGradiente,FxColorido,FxExtraColorido,FxTransFan,FxSumaSaturada,FxSumaSaturadaColor,FxTablaColores);
   TEstiloTransparencia=(etNinguno,etNegro,etMagenta);
   TAlineacionX=(AxIzquierda,AxCentro,AxDerecha);
   TAlineacionY=(AyArriba,AyCentro,AyAbajo);
   TTextoDDraw=class(Tobject)
   private
     fancho,falto:integer;
     bitmapTexto:PByte;
     fTextHeight:integer;//sólo para leer.
     colores:array[0..3] of word;
     fColor:Tcolor;
     function anchoLetra(letra:char):integer;
     procedure setColor(Color:Tcolor);
     procedure BltIndex(Controlador:pointer;Pitch:integer;const DestR,OrigR:Trect);
   public
     LimitesTexto:Prect;
     alineacionX:TalineacionX;
     alineacionY:TalineacionY;
     SuperficieDestino:IDirectDrawSurface7;
     destructor destroy; override;
     property TextHeight:integer read fTextHeight;
     property color:Tcolor read fColor write setColor;
     constructor create(Superficie:IDirectDrawSurface7;const filename:string);
     function TextOut(X,Y:integer;const texto:string):Trect;
     function TextWidth(const texto:string):integer;
     function ExtraerTexto(var texto:string; ancho:integer):string;
   end;

  //Igual que en Objetos!!
  TbrilloFxObjeto=(bfxNinguno,bfxMagico,bfxMalvado,bfxVenenoso,bfxOscuro,
    bfxGris,bfxBrilloGris,bfxFuegoInterno,bfxCongelado,bfxDesactivado,
    bfxBrilloGrisDesactivado);
  TImagen40= class(TBitmap)
  private
    { Private declarations }
    MC_resalte_1_32:array[0..31] of word;
    MC_resalte_2_32:array[0..31] of word;
    MC_resalte_1_128:array[0..127] of byte;
    MC_resalte_2_128:array[0..127] of word;
    MC_resalte_3_128:array[0..127] of word;
    MC_Grises_128:array[0..127] of word;
    MC_BrilloGris_128:array[0..127] of word;
  public
    { Public declarations }
    constructor create; override;
    procedure copiarImagen(x,y:integer;img:TBitmap;efecto:TBrilloFxObjeto);
    procedure copiarTransMagenta(img:TBitmap);
  end;

  function InicializarDirectDraw(HWNDventana:HWND):Hresult;
  procedure InicializarEfectos(const directorio:string);
  function CrearSuperficieOculta(var superficie:IDirectDrawSurface7;ancho,alto:integer;Transparencia:TEstiloTransparencia):hresult;
  procedure CopiarCanvasASuperficie(SuperficieDD:IDirectDrawSurface7;DestX,DestY,ancho,alto:integer;handleO,OrigX,OrigY:integer);
  procedure CopiarSuperficieACanvas(HandleD,DestX,DestY,ancho,alto:integer;SuperficieO:IDirectDrawSurface7;OrigX,OrigY:integer);
  procedure CopyTransMagenta(SupDest:IDirectDrawSurface7;DestX,DestY,Ancho,Alto:integer;Origen:Tbitmap;OrigX,OrigY:integer);
  procedure RealizarZoom;
  procedure FinalizarDirectDraw;
//FX Generales:
  procedure BltFx(const DestR:Trect;origen:Tbitmap;const OrigR:Trect;colorFx:Tcolor;estilo:TFxAlpha);
  procedure BltTablaColor(const DestR:Trect;origen:Tbitmap;const OrigR:Trect;espejo,transparente:bytebool);
  procedure PrepararTablaColores(Quintuple0,Quintuple1:integer);
  procedure BltTrans(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
  procedure BltAntiAlisado(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
  procedure BltTransFan(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
  procedure BltMejorado(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);

  procedure BltTileEspejoNoColorKey(const DestR:Trect;const OrigR:Trect);

  procedure BltFxColor(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool;Color:integer);
  procedure BltFxMascara(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool;Color:integer);
//FX Específicos
  function AplicarFXAmbiental(intensidad:byte;TipoEfecto:TClimaAmbiental;SubTipoEfecto:TFxNocturno):bytebool;
  procedure AplicarLluviaAmbiental(contadorExterno:integer;intensidad:byte);
  procedure AplicarNieveAmbiental(contadorExterno:integer;intensidad:byte);
  procedure AplicarFXAmbientalRayo;
  procedure BltAlphaTile(const DestR:Trect;const OrigR:Trect;nro_mezcla:byte);
  procedure BltAlpha(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool;alpha:byte);
  procedure BltMarcoDialogo(const DestR:Trect);
  procedure BltZonaFrontera(const DestR:Trect);
//Otros entre Tbitmaps
  function blt0TransTablaColores(BitmapDes:Tbitmap;XDes,YDes,Ancho,Alto:integer;BitmapOrg:Tbitmap;XOrg,YOrg:integer):boolean;
  function ColorDeLaTabla(indice:byte):Tcolor;

  procedure BltLiquido(const DestR:Trect;const OrigR:Trect;paleta,tipo:byte);
  procedure BltAlphaLiquido(const DestR:Trect;const OrigR:Trect;nro_mezcla:byte;paleta,tipo:byte);

//Creacion de bitmaps:
  function CrearDeGDD8bits(const filename:string):Tbitmap;
  function CrearDeGDD(const filename:string):Tbitmap;
  function CrearDeJDD(const filename:string):Tbitmap;
  function CrearSuperficieDeJDD(var superficie:IDirectDrawSurface7;const filename:string):boolean;
  function CrearSuperficieDeBMP(var superficie:IDirectDrawSurface7;const filename:string):boolean;
  function CrearBackBuffer16Bits(ancho,alto:integer):Tbitmap; //16 bits
//Auxiliares de control y optimización
  function EstaEnPantalla(var rDestino,rOrigen:Trect;const espejo:bytebool):bytebool;
  function EstaEnInterior(var rDestino,rOrigen:Trect;const rLimites:Trect):bytebool;
  procedure CambiarAreaDibujable(zoom,PanelDerechoActivo:longbool);
  procedure WaitForScanLine(minScanLine, maxScanLine:integer);
  function ScanLineActual():integer;
  function FrecuenciaDelMonitor():integer;

  function RestaurarResolucionInicial():boolean;
  function CambiarAResolucionHD():boolean;

var
  SuperficieRender:IDirectDrawSurface7;//Superficie de render.
  SuperficieTerreno:IDirectDrawSurface7;
  TextoDDraw:TTextoDDraw;//Objeto para dibujar las letras con estilo.
  //Usando sólo uno para todas las superficies, sólo un hilo de proceso debe encargarse de dibujar todo
  DescSuperficieLockUnlock:TDDSURFACEDESC2;
  G_DDFuncionando:boolean;
  Conta_universal:^integer;
  //Para colores en 8 bits :(
  TablaDeColorIndexado676:array[0..255] of integer;

const
  mskTrans=$F7DF;

implementation
uses JPEG,sysutils;

const
  altoLluvia=256;
  altoNieve=128;
  altoTotalLluvia=altoLluvia*4;
  altoTotalNieve=altoNieve*4;
  anchoLluvia=160;
  ANCHO_LIQUIDO=144;

  log2altoLluvia=8;
  log2altoNieve=7;
  mskBitsAltos=dword($8410);
  mskCuartoAlpha=dword($E79C);
  ClaveColMagenta=$F81F;
  mskRojo=dword($F800);
  mskTransRojo=dword($F000);
  mskVerde=dword($07E0);
  desRojoPl=8;//Desplazador
  desVerdePl=3;//Desplazador
  desVerde=5;//Desplazador
  desRojo=8;
  desRojo5bits=11;
  mskAzul=dword($001F);
  mskMagenta=dword(mskAzul or mskRojo);
  mskAmarillo=dword(mskRojo or mskVerde);
  mskVerde16=dword($07E0);
  mskTransAmarillo=dword((mskTrans or mskAzul) xor mskAzul);
  mskTransMagenta=dword((mskTrans or mskVerde) xor mskVerde);
  mskTransCyan=dword((mskTrans or mskRojo) xor mskRojo);
  mskTransVerde=dword((mskTrans or mskMagenta) xor mskMagenta);
  ValoresColor7N:array[0..6] of byte=(0,63,127,160,192,224,255);
  ValoresColor6N:array[0..5] of byte=(0,63,127,170,212,255);

var
    ImagenAmbiental,ImagenLluvia,ImagenNieve,ImagenLiquido,enlaceMosaico,pisoMosaico:PLinea8bits;//enlaces de mosaicos
    TablaR:array[0..255] of byte;
    TablaG:array[0..255] of byte;
    TablaB:array[0..255] of byte;
    // 64 colores, 4 niveles, 4 paletas:
    Paleta_Liquidos:array[0..1023] of word;
    //Para Indexación de colores
    TablaIndexadaDeColores:array[0..255] of word;//Visible para BltTablaColor
    TablaEscala:array[0..63] of byte;//Para crear la tabla indexada de colores;
    QuintupleColores0,QuintupleColores1:integer;//Colores de los estandartes. (30 bits en cada integer)
    SumaSatB:array[0..95] of byte;
    SumaSatR,SumaSatG:array[0..510] of word;
    ObjetoDirectDraw: IDirectDraw7;
    DDBLTFX_H:TDDBltFX; //especificaciones fx espejo horizontal
    Limites_Lienzo:Trect;//Limites del area dibujable de la superficie de render.
    g_cambiadoAResolucionHd:boolean;

//Funciones auxiliares
function ReducirBitsColor(Color32:Tcolor):word;
begin
  result:=(Color32 and $0000F8) shl 8 or
            (Color32 and $00FC00) shr 5 or
            (Color32 and $F80000) shr 19;
end;

function FrecuenciaDelMonitor():integer;
{
var dmPrimary: DEVMODE;
const ENUM_CURRENT_SETTINGS = $ffffffff;
begin
  ZeroMemory(@dmPrimary, sizeof(dmPrimary));
  dmPrimary.dmSize:= sizeof(dmPrimary);
  if EnumDisplaySettings(nil, ENUM_CURRENT_SETTINGS, dmPrimary) then
    result:=dmPrimary.dmDisplayFrequency
  else
    result:=0;
end;
}
var freq:dword;
begin
  ObjetoDirectDraw.GetMonitorFrequency(freq);
  result:=freq;
end;

function ScanLineActual():integer;
var scanLine:dword;
    hr:HResult;
begin
  hr:= ObjetoDirectDraw.GetScanLine(scanLine);
  if hr = DDERR_VERTICALBLANKINPROGRESS then
    result:= -1
  else if hr = DD_OK then
    result:= scanLine
  else
    result:= -2;
end;

procedure WaitForScanLine(minScanLine, maxScanLine:integer);
var scanLine:dword;
    hr:HResult;
begin
  scanLine:= 0;
  while true do
  begin
    hr:=ObjetoDirectDraw.GetScanLine(scanLine);
    if (minScanLine<0) and (hr=DDERR_VERTICALBLANKINPROGRESS) then break;
    if (hr=DD_OK) and (integer(scanLine)>=minScanLine) and ((integer(scanLine)<=maxScanLine)) then break;
    if (hr<>DD_OK) and (hr<>DDERR_VERTICALBLANKINPROGRESS) then exit;
    Windows.Sleep(1);
  end;
end;

//**********************************************************
//                     DIRECT DRAW
//*********************************************************

function CrearSuperficieOculta(var superficie:IDirectDrawSurface7;ancho,alto:integer;Transparencia:TEstiloTransparencia):hresult;
//result es del tipo IDirectDrawSurface, para los efectos un puntero, igual que dd.
var    descripcionSuperficie:TDDSURFACEDESC2;
       ColorTransparente:TDDCOLORKEY;
begin
   FillChar(descripcionSuperficie, SizeOf(descripcionSuperficie),#0);
   with descripcionSuperficie do
   begin
     dwSize := sizeof(descripcionSuperficie);
     dwFlags := DDSD_CAPS or DDSD_HEIGHT or DDSD_WIDTH
       or DDSD_PIXELFORMAT;
     ddsCaps.dwCaps:=DDSCAPS_OFFSCREENPLAIN or DDSCAPS_SYSTEMMEMORY;
     dwWidth := ancho;
     dwHeight := alto;

     ddpfPixelFormat.dwSize:= sizeof(TDDPixelFormat_DX6);
     ddpfPixelFormat.dwFlags:= DDPF_RGB;
     ddpfPixelFormat.dwRGBBitCount:= 16;
     ddpfPixelFormat.dwRBitMask:= mskRojo;
     ddpfPixelFormat.dwGBitMask:= mskVerde;
     ddpfPixelFormat.dwBBitMask:= mskAzul;
   end;
   result:=ObjetoDirectDraw.CreateSurface(descripcionSuperficie,superficie,nil);
   if result=DD_OK then
     if transparencia<>etNinguno then
     begin
       if transparencia=etNegro then
         ColorTransparente.dwColorSpaceLowValue :=$0
       else
         ColorTransparente.dwColorSpaceLowValue :=ClaveColMagenta;
       ColorTransparente.dwColorSpaceHighValue :=ColorTransparente.dwColorSpaceLowValue;
       superficie.SetColorKey(DDCKEY_SRCBLT,@ColorTransparente);
     end;
end;

function InicializarDirectDraw(HWNDventana:HWND):Hresult;
var objetoTemporal:IDirectDraw;
begin
  g_cambiadoAResolucionHd:=false;
  //Objeto Principal
  result:=DirectDrawCreate(Nil,ObjetoTemporal,Nil);//Realiza un flick de pantalla
  if result<>DD_OK then exit;
  //Objeto especifico a DX7.
  result:=ObjetoTemporal.QueryInterface(IID_IDirectDraw7, ObjetoDirectDraw);
  if result<>DD_OK then
  begin
    ObjetoTemporal:=nil;
    exit;
  end;

  //Colocar en modo cooperativo:
  result:=ObjetoTemporal.SetCooperativeLevel(HWNDventana,
    DDSCL_NORMAL or DDSCL_NOWINDOWCHANGES);
  if (result<>DD_OK) then
  begin
    ObjetoDirectDraw:=nil;
    ObjetoTemporal:=nil;
    exit;
  end;
  //Ya no necesitamos el objeto temporal:
  ObjetoTemporal:=nil;

//Creando superficie de render:
  result:=CrearSuperficieOculta(SuperficieRender,ancho_dd,alto_dd,etNinguno);
  if(result<>DD_OK) then
  begin
    ObjetoDirectDraw:=nil;
    exit;
  end;

  //Efectos:
  //Espejo Horizontal:
  With DDBLTFX_H do
  begin
    FillChar(DDBLTFX_H, SizeOf(DDBLTFX_H),#0);//Llena de ceros.
    dwSize:= sizeof(DDBLTFX_H);
    dwDDFX:=DDBLTFX_MIRRORLEFTRIGHT;
  end;
  //Limites iniciales:
  Limites_Lienzo:=Rectangulo_Origen;
  //Preparar la descripción de la superficie de Render para lock/unlock:
  FillChar(DescSuperficieLockUnlock, SizeOf(DescSuperficieLockUnlock),#0);//Llena de ceros.
  DescSuperficieLockUnlock.dwSize := sizeof(DescSuperficieLockUnlock);
  DescSuperficieLockUnlock.dwFlags := DDSD_LPSURFACE or DDSD_PITCH;
  result:=DD_Ok;
end;

procedure InicializarEfectos(const directorio:string);
var temp:TBitmap;
    i,j,c:integer;
    lindata:Plinea8bits;
  procedure PrepararMancha(data:Plinea8bits);
  const RATIO_FACTOR = ancho_dd / alto_dd;
        SCALE_FACTOR = 0.27;
        SCALE_BASE=250;
  var x,y,c:integer;
      alto,ancho,alto2,ancho2:integer;
      tem:integer;
      caly:single;
  begin
    alto:=alto_dd;
    alto2:=alto shr 1;
    ancho:=ancho_dd;
    ancho2:=ancho shr 1;
    x:=0;
    for y:=0 to alto-1 do
    begin
      caly:=sqr((alto2-y)*RATIO_FACTOR);
      for c:=0 to ancho-1 do
      begin
        tem:=SCALE_BASE-round(sqrt(sqr(ancho2-c)+caly)*SCALE_FACTOR);
        tem:=tem+XRandom(7)+XRandom(6)+XRandom(6);
        if tem<0 then tem:=0 else if tem>255 then tem:=255;
        data[x]:=tem;
        inc(x);
      end;
    end;
  end;
begin
  TextoDDraw:=TTextoDDraw.create(SuperficieRender,directorio+'letras'+ExtArc);

  //Terreno
  CrearSuperficieDeJDD(SuperficieTerreno,directorio+'terreno'+ExtArc2);

//Imagen para noche
  getmem(ImagenAmbiental,ancho_dd*alto_dd);
  PrepararMancha(imagenAmbiental);

//Imagen para la lluvia
  getmem(ImagenLluvia,altoTotalLluvia*anchoLluvia);
  temp:=CrearDeGDD8bits(directorio+'llu'+ExtArc);
  c:=0;
  for j:=0 to altoTotalLluvia-1 do
  begin
    lindata:=Temp.ScanLine[j];
    for i:=0 to anchoLluvia-1 do
    begin
      imagenLluvia[c]:=(lindata[i] shr 1) and $3F;
      inc(c);
    end;
  end;
  temp.free;
//Imagen para nieve
  getmem(ImagenNieve,altoTotalNieve*anchoLluvia);
  temp:=CrearDeGDD8bits(directorio+'nie'+ExtArc);
  c:=0;
  for j:=0 to altoTotalNieve-1 do
  begin
    lindata:=Temp.ScanLine[j];
    for i:=0 to anchoLluvia-1 do
    begin
      imagenNieve[c]:=lindata[i];
      inc(c);
    end;
  end;
  temp.free;
  //Imagen para liquidos
  temp:=CrearDeGDD8bits(directorio+'liq'+ExtArc);
  getmem(ImagenLiquido,temp.Height*temp.Width);
  c:=0;
  for j:=0 to temp.Height-1 do
  begin
    lindata:=Temp.ScanLine[j];
    for i:=0 to temp.Width-1 do
    begin
      ImagenLiquido[c]:=lindata[i];
      inc(c);
    end;
  end;
  temp.free;
  // Paleta de liquidos
  c:=0;
  for i:=0 to 255 do
  begin
    j:=(i and $3F)*((i shr 6)+3) shr 5;
    Paleta_Liquidos[c]:=(3+(j shr 2)) or ((4+(j shr 1)) shl 5) or ((2+(j shr 2)) shl 11);
    inc(c);
  end;
  for i:=0 to 255 do
  begin
    j:=(i and $3F)*((i shr 6)+3) shr 5;
    Paleta_Liquidos[c]:=((0+(j shr 0)) shl 5) or ((16+(j shr 2)) shl 11);
    inc(c);
  end;
  for i:=0 to 255 do
  begin
    j:=(i and $3F)*((i shr 6)+3) shr 5;
    Paleta_Liquidos[c]:=(8+(j shr 2)) or ((22+(j shr 1)) shl 5) or ((12+(j shr 2)) shl 11);
    inc(c);
  end;
  for i:=0 to 255 do
  begin
    j:=(i and $3F)*((i shr 6)+2) shr 4;
    Paleta_Liquidos[c]:=(13+(j shr 2)) or ((18+(j shr 1)) shl 5) or ((5+(j shr 2)) shl 11);
    inc(c);
  end;
  //Imagen para elevacion de mosaicos
  temp:=CrearDeGDD8bits(directorio+'te'+ExtArc);
  getmem(pisoMosaico,temp.Height*temp.Width);
  c:=0;
  for j:=0 to temp.Height-1 do
  begin
    lindata:=Temp.ScanLine[j];
    for i:=0 to temp.Width-1 do
    begin
      pisoMosaico[c]:=lindata[i];
      inc(c);
    end;
  end;
  temp.free;
  //Imagen para enlaces de mosaicos
  temp:=CrearDeGDD8bits(directorio+'ti'+ExtArc);
  getmem(enlaceMosaico,temp.Height*temp.Width);
  c:=0;
  for j:=0 to temp.Height-1 do
  begin
    lindata:=Temp.ScanLine[j];
    for i:=0 to temp.Width-1 do
    begin
      enlaceMosaico[c]:=lindata[i];
      inc(c);
    end;
  end;
  temp.free;
  //Para suma saturada:
  for i:=0 to 510 do
  begin
    if i<255 then j:=i else j:=255;
    SumaSatR[i]:=(j shl desRojoPl) and mskRojo;
    SumaSatG[i]:=(j shl desVerdePl) and mskVerde;
  end;
  for i:=0 to 31 do
    SumaSatB[i]:=i;
  for i:=32 to 95 do
    SumaSatB[i]:=31;
  //Para preparar las tablas de colores indexados:
  for i:=0 to 15 do
    TablaEscala[i]:=0;
  for i:=0 to 15 do
    TablaEscala[16+i]:=i*8+8;
  for i:=0 to 15 do
    TablaEscala[32+i]:=i*8+64;
  for i:=0 to 15 do
    TablaEscala[48+i]:=i*8+135;
  //Para preparar la tabla de colores de 8 bits.
  for i:=0 to 5 do
    for j:=0 to 6 do
      for c:=0 to 5 do
        TablaDeColorIndexado676[i*42+j*6+c]:=
          ValoresColor6N[i] shl 16+ValoresColor7N[j] shl 8+ValoresColor6N[c];
  for i:=252 to 255 do
    TablaDeColorIndexado676[i]:=0;
end;

procedure FinalizarDirectDraw;
begin
  freemem(enlaceMosaico);
  freemem(pisoMosaico);
  freemem(ImagenLiquido);
  freemem(ImagenNieve);
  freeMem(ImagenLluvia);
  freeMem(ImagenAmbiental);
  superficieTerreno:=nil;
  TextoDDraw.free;
  //Eliminar objetos reduciendo el número de referencias a 0.
  SuperficieRender:=nil;
  ObjetoDirectDraw:=nil;
end;

procedure CopiarCanvasASuperficie(SuperficieDD:IDirectDrawSurface7;DestX,DestY,ancho,alto:integer;handleO,OrigX,OrigY:integer);
var HDCSuperficie:HDC;
begin
  if SuperficieDD.GetDC(HDCSuperficie)=DD_OK then
  begin
    bitblt(HDCSuperficie,DestX,DestY,ancho,alto,handleO,OrigX,OrigY,SRCCOPY);
    SuperficieDD.ReleaseDC(HDCSuperficie);
  end;
end;

procedure CopiarSuperficieACanvas(HandleD,DestX,DestY,ancho,alto:integer;SuperficieO:IDirectDrawSurface7;OrigX,OrigY:integer);
var HDCSuperficie:HDC;
begin
  if SuperficieO.GetDC(HDCSuperficie)=DD_OK then
  begin
    bitblt(handleD,DestX,DestY,ancho,alto,HDCSuperficie,OrigX,OrigY,SRCCOPY);
    SuperficieO.ReleaseDC(HDCSuperficie);
  end;
end;

procedure RealizarZoom;
//  Hace esto: (pero casi el doble de rápido):
//  SuperficieRender.Blt(@rectangulo_Origen,SuperficieRender,@rectangulo_OrigenZ,DDBLT_ASYNC,nil);
const
  //Solo para dimensiones de pantalla múltiplo de 8, ancho y alto
  mitad_ancho_dd=ancho_dd shr 1;
  mitad_alto_dd=alto_dd shr 1;
  Mitad_menos1_ancho_dd=mitad_ancho_dd-1;
  Cuarto_menos1_alto_dd=(alto_dd shr 2)-1;
  Doble_alto_dd=alto_dd shl 1;
  Tres_medios_ancho_dd=((ancho_dd*3) shr 1);
  Tres_medios_menos2_alto_dd=((alto_dd*3) shr 1)-2;

var
    contaD,contaO,base,contaTemp:pointer;
    y,finX,finY,AnchoFaltante:integer;
begin
//SuperficieRender
  if SuperficieRender.lock(@Rectangulo_Origen,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    base:=DescSuperficieLockUnlock.lpSurface;
    AnchoFaltante:=Ancho_DD;//mitad *2 bytes por pixel=completo
    finY:=Cuarto_menos1_alto_dd;//1/4 -1
    contaD:=base;
    integer(contaO):=integer(base)+mitad_ancho_dd+mitad_alto_dd*Ancho_dd;
    for y:=0 to finY do
    begin
      contaTemp:=contaD;
      finx:=integer(contaD)+(mitad_ancho_dd shl 2);
      while integer(contaD)<finx do
      begin
        word(contaD^):=word(contaO^);
        inc(integer(contaD),2);
        word(contaD^):=word(contaO^);
        inc(integer(contaD),2);
        inc(integer(contaO),2);
      end;
      inc(integer(contaO),AnchoFaltante);//Ancho faltante
      finx:=integer(contaD)+(mitad_ancho_dd shl 2);
      while integer(contaD)<finx do
      begin
        integer(contaD^):=integer(contaTemp^);
        inc(integer(contaD),4);
        inc(integer(contaTemp),4);
      end;
    end;
    //Coordenadas inferior derecha de la pantalla con zoom.
    integer(contaO):=integer(base)+Tres_medios_ancho_dd+Tres_medios_menos2_alto_dd*Ancho_dd;
    integer(contaD):=integer(base)+Doble_alto_dd*Ancho_dd;
    for y:=0 to finY  do
    begin
      contaTemp:=contaD;
      finx:=integer(contaD)-(mitad_ancho_dd shl 2);
      while integer(contaD)>finx do
      begin
        dec(integer(contaO),2);
        dec(integer(contaD),2);
        word(contaD^):=word(contaO^);
        dec(integer(contaD),2);
        word(contaD^):=word(contaO^);
      end;
      dec(integer(contaO),AnchoFaltante);//Ancho faltante
      finx:=integer(contaD)-(mitad_ancho_dd shl 2);
      while integer(contaD)>finx do
      begin
        dec(integer(contaD),4);
        dec(integer(contaTemp),4);
        integer(contaD^):=integer(contaTemp^);
      end;
    end;
    SuperficieRender.unlock(@Rectangulo_Origen);
  end;
end;

function EstaEnInterior(var rDestino,rOrigen:Trect;const rLimites:Trect):bytebool;
// Determina si la imagen que debe ser dibujada aparece en pantalla y modifica
// los rectángulos origen y destino si estos exceden las dimensiones adecuadas.
begin
with rDestino do
 if (left<rLimites.right) and (right>=rLimites.left) and
   (top<rLimites.bottom) and (bottom>=rLimites.top) then
 begin
   result:=true;
   //Reducir el tamaño del cuadro si sale de pantalla
   if right>rLimites.right then
   begin
     rorigen.Right:=rOrigen.Left+rLimites.right-left;//!!!!
     right:=rLimites.right;
   end;
   if left<rLimites.left then
   begin
     dec(rorigen.Left,left-rLimites.left);
     left:=rLimites.left;
   end;
   if bottom>rLimites.bottom then
   begin
     rorigen.Bottom:=rOrigen.top+rLimites.bottom-top;
     bottom:=rLimites.bottom;
   end;
   if top<rLimites.top then
   begin
     dec(rorigen.top,top-rLimites.top);
     top:=rLimites.top;
   end;
 end
 else
   result:=false;
end;

//Más general
function EstaEnPantalla(var rDestino,rOrigen:Trect;const espejo:bytebool):bytebool;
//  Determina si la imagen que debe ser dibujada aparece en pantalla y modifica
//los rectángulos origen y destino si estos exceden las dimensiones adecuadas.
begin
with rDestino do
 if (left<Limites_Lienzo.right) and (right>=Limites_Lienzo.left) and
   (top<Limites_Lienzo.bottom) and (bottom>=Limites_Lienzo.top) then
 begin
   result:=true;
   //Reducir el tamaño del cuadro si sale de pantalla
   if espejo then
   begin//caso del espejo
     if right>Limites_Lienzo.right then
     begin
       right:=Limites_Lienzo.right;
       rorigen.Left:=rOrigen.Right-right+left;
     end;
     if left<Limites_Lienzo.left then
     begin
       left:=Limites_Lienzo.left;
       rorigen.right:=rOrigen.left+right-left;
     end;
   end
   else
   begin// caso normal
     if right>Limites_Lienzo.right then
     begin
       right:=Limites_Lienzo.right;
       rorigen.Right:=rOrigen.Left+right-left;
     end;
     if left<Limites_Lienzo.left then
     begin
       left:=Limites_Lienzo.left;
       rorigen.Left:=rOrigen.Right-right+left;
     end;
   end;
   if bottom>Limites_Lienzo.bottom then
   begin
     bottom:=Limites_Lienzo.bottom;
     rorigen.Bottom:=rOrigen.top+bottom-top;
   end;
   if top<Limites_Lienzo.top then
   begin
     top:=Limites_Lienzo.top;
     rorigen.top:=rorigen.Bottom-bottom+top;
   end;
 end
 else
   result:=false;
end;

function QuitarNegativos(n:integer):byte; register;
//si n<0, n:=0
// n=EAX
asm
  cmp eax,0// compara n con 0
  jge @fin
  xor eax,eax
  @fin:
end;

function IntegerAByte(n:integer):byte; register;
//si n<0 entonces n:=0; Si n>255 entonces n:=255
// n=EAX
asm
  cmp eax,0// compara n con 0
  jge @SinNegativos
  xor eax,eax
  jmp @fin
  @SinNegativos:
  cmp eax,255//compara n con 255
  jle @fin
  mov eax,255
  @fin:
{//sin saltos pero tiene más instrucciones y es más lento :(
  xor    ecx, ecx
  test   eax, eax
  setge  cl
  neg    ecx
  and    eax, ecx
  cmp    eax, 255
  setle  cl
  neg    cl
  and    cl, al
  cmp    eax, 255
  setg   al
  neg    al
  or     al, cl
}
end;

function blt0TransTablaColores(BitmapDes:Tbitmap;XDes,YDes,Ancho,Alto:integer;BitmapOrg:Tbitmap;XOrg,YOrg:integer):boolean;
var linea:^Tlinea16bits;
    lineao:^Tlinea8bits;
    i,j,Ajuste:integer;
    org,dst:Trect;
begin
  dst.left:=xDes;
  dst.top:=yDes;
  dst.right:=xDes+Ancho;
  dst.bottom:=yDes+Alto;
  org.left:=XOrg;
  org.top:=YOrg;
  org.right:=XOrg+ancho;
  org.bottom:=YOrg+alto;
  result:=EstaEnInterior(dst,org,BitmapDes.Canvas.ClipRect);
  if not result then exit;
  dec(Ancho);
  Ajuste:=XDes-XOrg;
  for j:=0 to Alto-1 do
  begin
    lineao:=BitmapOrg.scanline[j+YOrg];
    linea:=BitmapDes.scanline[j+YDes];
    for i:=XOrg to Ancho+XOrg do
      if lineao[i]<>$0 then
        linea[i+Ajuste]:=TablaIndexadaDeColores[lineao[i]];
  end;
end;

function AplicarFXAmbiental(intensidad:byte;TipoEfecto:TClimaAmbiental;SubTipoEfecto:TFxNocturno):bytebool;
//intensidad: 0=min,255=max
var
    base,finy,
    anchoFaltante,anchoFaltanteOrigen,anchoOrigen,i,j:integer;
    contaOrigen,posicionD:pointer;
    res:hresult;
const
    maskRojo15Verde = (mskRojo shl 15) or mskVerde;
begin
  result:=false;
  res:=SuperficieRender.lock(@Rectangulo_Origen,DescSuperficieLockUnlock,DDLOCK_WAIT,0);
  if res=DD_OK then
  begin
    With DescSuperficieLockUnlock,Limites_lienzo do
    begin
      posicionD:=pointer(integer(lpSurface)+(left+top*Ancho_dd) shl 1);
      contaOrigen:=pointer(Integer(ImagenAmbiental)+(left+top*Ancho_dd));
      anchoFaltanteOrigen:=(Ancho_dd-(right-left));
      anchoFaltante:=anchoFaltanteOrigen shl 1;
      finy:=Integer(ImagenAmbiental)+bottom*ancho_dd;
      anchoOrigen:=right-left;
      base:=(256-intensidad);
    end;
    if TipoEfecto=CL_BRUMA then
    begin
      {
      while integer(contaOrigen)<finy do
      begin
        finx:=integer(contaOrigen)+anchoOrigen;
        while integer(contaOrigen)<finx do
        begin
          dato2:=integer(byte(contaOrigen^)+base);
          if dato2<256 then
          begin
            dato:=integer(not word(posicionD^));
            i:=(((((dato and integer(mskRojo)) shl 2) or (dato and integer(mskAzul)))*dato2) shr 8);
            word(posicionD^):=not (
              (i and integer(mskAzul)) or ((i shr 2)and integer(mskRojo)) or
              (((dato and integer(mskVerde))*dato2 shr 8) and integer(mskVerde)));
          end;
          inc(integer(posicionD),2);
          inc(integer(contaOrigen));
        end;
        inc(integer(posicionD),AnchoFaltante);
        inc(integer(contaOrigen),AnchoFaltanteOrigen);
      end;
      }
      asm
        push edi;
        push esi;
        push ebx;

        add base,24

        mov ebx,contaOrigen
        mov esi,posicionD
        @inicioCicloY:
         cmp ebx,finy
         jae @finCicloY
            mov edi,ebx
            add edi,anchoOrigen
            @inicioCicloX:
             cmp ebx,edi
             jae @finCicloX
                //**
                movzx ecx, byte ptr [ebx]//byte conta origen
                add ecx,base
                shr ecx,2
                cmp ecx,63
                ja @siguiente
                movzx edx, word ptr [esi]//word destino
                not edx
                // eax: 6(verde) 10 5(rojo) 6 5(azul)
                mov eax,edx
                and eax,mskMagenta
                and edx,mskverde
                shl edx,21
                or eax,edx
                mul ecx // edx:eax = eax*ecx(pixel de mancha)
                // verde en edx, rojo y azul en eax : 10 5(rojo) 6 5(azul) 6
                shl edx,5
                shr eax,6
                and eax,mskMagenta
                or edx,eax
                // move result to memory
                not edx
                mov word ptr [esi],dx
                @siguiente:
                //**
                inc ebx
                add esi,2
                jmp @inicioCicloX
            @finCicloX:
            add esi,anchoFaltante
            add ebx,anchoFaltanteOrigen
            jmp @inicioCicloY
        @finCicloY:

        pop ebx;
        pop esi;
        pop edi;
      end;
    end
    else// Efectos nocturnos
      case SubTipoEfecto of
        FxNocheAzul:
        begin
          for i:=0 to 255 do
          begin
            j:=(i+base) shr 2;
            if j>=64 then
              TablaG[i]:=64
            else
              TablaG[i]:=j;
          end;
{
          while integer(contaOrigen)<finy do
          begin
            finx:=integer(contaOrigen)+anchoOrigen;
            while integer(contaOrigen)<finx do
            begin
              dato2:=TablaG[byte(contaOrigen^)];
              dato:=word(posicionD^);
              if dato2>32 then
                word(posicionD^):=
                  (dato and mskAzul) or
                  ((dato and mskVerde)*dato2 shr 6 and mskVerde) or
                  ((dato shr 6)*dato2 and mskRojo) //1.67
              else
                word(posicionD^):=((dato and mskTransAmarillo) shr 1) or (dato and mskAzul);
              inc(integer(posicionD),2);
              inc(integer(contaOrigen));
            end;
            inc(integer(posicionD),AnchoFaltante);
            inc(integer(contaOrigen),AnchoFaltanteOrigen);
          end;
}
          asm
            push edi
            push esi
            push ebx

            mov ebx,contaOrigen
            mov esi,posicionD
            @inicioCicloY:
             cmp ebx,finy
             jae @finCicloY
                mov edi,ebx
                add edi,anchoOrigen
                @inicioCicloX:
                 cmp ebx,edi
                 jae @finCicloX
                    movzx edx, byte ptr [ebx]//byte conta origen
                    movzx edx, byte ptr [edx+tablaG]
                    movzx eax, word ptr [esi]//word destino
                    mov ecx, eax
                    cmp edx,32
                    jle @simple

                    shl eax, 15
                    xor eax, ecx
                    and ecx, mskAzul
                    and eax, maskRojo15Verde
                    mul edx
                    shl edx, 11
                    or ecx, edx
                    shr eax, 6
                    and eax, mskVerde
                    jmp @siguiente

                    @simple:
                    and eax, mskTransAmarillo
                    shr eax, 1
                    and ecx, mskAzul

                    @siguiente:
                    or ecx, eax
                    mov word ptr [esi],cx
                    add esi,2
                    inc ebx
                    jmp @inicioCicloX
                @finCicloX:
                add esi,anchoFaltante
                add ebx,anchoFaltanteOrigen
                jmp @inicioCicloY
            @finCicloY:

            pop ebx
            pop esi
            pop edi
          end;

        end;
        FXInfravision:
        begin
          for i:=0 to 255 do
          begin
            j:=(i+base) shr 2;
            if j>=64 then
              TablaB[i]:=64
            else
              if j<32 then
                TablaB[i]:=32
              else
                TablaB[i]:=j;
          end;
         // tabla:=@TablaB;
//          j:=integer(@(TablaG[0]));
{          while integer(contaOrigen)<finy do
          begin
            finx:=integer(contaOrigen)+anchoOrigen;
            while integer(contaOrigen)<finx do
            begin
              dato:=word(posicionD^);
              dato2:=TablaG[byte(contaOrigen^)];
              if dato2>32 then
                word(posicionD^):=
                  (dato and mskMagenta) or
                  ((dato and mskVerde)*dato2 shr 6 and mskVerde)
              else
                word(posicionD^):=(dato and mskMagenta) or ((dato and mskTransVerde) shr 1);
              inc(integer(posicionD),2);
              inc(integer(contaOrigen));
            end;
            inc(integer(posicionD),AnchoFaltante);
            inc(integer(contaOrigen),AnchoFaltanteOrigen);
          end;}
          asm
            push edi
            push esi
            push ebx

            mov ebx,contaOrigen
            mov esi,posicionD
            @inicioCicloY:
             cmp ebx,finy
             jae @finCicloY
                mov edi,ebx
                add edi,anchoOrigen
                @inicioCicloX:
                 cmp ebx,edi
                 jae @finCicloX
                    movzx eax, word ptr [esi]//word destino
                    mov ecx, eax
                    and eax, mskVerde
                    and ecx, mskMagenta
                    movzx edx, byte ptr [ebx]//byte conta origen
                    movzx edx, byte ptr [edx+tablaB]
                    mul edx
                    shr eax, 6
                    and eax, mskVerde
                    or ecx, eax
                    mov word ptr [esi],cx

                    add esi,2
                    inc ebx
                    jmp @inicioCicloX
                @finCicloX:
                add esi,anchoFaltante
                add ebx,anchoFaltanteOrigen
                jmp @inicioCicloY
            @finCicloY:

            pop ebx
            pop esi
            pop edi
          end;

        end;
      end;//case
    SuperficieRender.unlock(@Rectangulo_Origen);
    result:=true;
  end;
end;

procedure PrepararTablaColores(Quintuple0,Quintuple1:integer);
  procedure PrepararQuinteto(colores,indiceBase:integer);
  var k,i,j,r,g,b:integer;
  begin
    for k:=indiceBase to indiceBase+4 do
    begin
      //extraer y pasar de 2 a 6 bits.
      r:=(colores and $30);
      g:=(colores and $0C) shl 2;
      b:=(colores and $03) shl 4;
      colores:=colores shr 6;//recorrer lo extraido
      j:=k shl 4;
      for i:=0 to 15 do
      begin
        TablaIndexadaDeColores[j+i]:=ReducirBitsColor(
          (TablaEscala[r+i]) or
          (TablaEscala[g+i] shl 8) or
          (TablaEscala[b+i] shl 16));
      end;
    end;
  end;
begin
  if (Quintuple0<>QuintupleColores0) then
  begin
    PrepararQuinteto(Quintuple0,0);
    QuintupleColores0:=Quintuple0;
  end;
  if (Quintuple1<>QuintupleColores1) then
  begin
    PrepararQuinteto(Quintuple1,5);
    QuintupleColores1:=Quintuple1;
  end;
end;

function ColorDeLaTabla(indice:byte):Tcolor;
begin
  result:=TablaIndexadaDeColores[((indice shl 4)+15) and $FF];
  result:=((result and mskAzul) shl 19) or
          ((result and mskVerde) shl 5) or
          ((result and mskRojo) shr 8);
end;

procedure BltTablaColor(const DestR:Trect;origen:Tbitmap;const OrigR:Trect;espejo,transparente:bytebool);
var x,y,anchoFaltante,ancho:integer;
    conta:pointer;
    linea:Plinea8bits;
    dato2:byte;
begin
  if SuperficieRender.lock(@Rectangulo_Origen,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    inc(integer(conta),(DestR.left+ancho_dd*DestR.top)*2);
    anchoFaltante:=(ancho_dd+origR.Left-origR.Right)*2;
    ancho:=(origR.Right-origR.Left)*2;//en bytes
    if espejo then
    begin
      inc(anchoFaltante,ancho);
      if transparente then
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           inc(integer(conta),ancho);
           for x:=origR.Left to origR.Right-1 do
           begin
             dec(integer(conta),2);
             dato2:=linea[x];
             if dato2<>0 then// 0=transparente
               word(conta^):=((word(conta^) and mskTrans)+(TablaIndexadaDeColores[dato2] and msktrans)) shr 1;
           end;
           inc(integer(conta),anchoFaltante);
        end
      else
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           inc(integer(conta),ancho);
           for x:=origR.Left to origR.Right-1 do
           begin
             dec(integer(conta),2);
             dato2:=linea[x];
             if dato2<>0 then// 0=transparente
               word(conta^):=TablaIndexadaDeColores[dato2];
           end;
           inc(integer(conta),anchoFaltante);
        end
    end
    else
      if transparente then
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Left to origR.Right-1 do
           begin
             dato2:=linea[x];
             if dato2<>0 then// 0=transparente
               word(conta^):=((word(conta^) and mskTrans)+(TablaIndexadaDeColores[dato2] and msktrans)) shr 1;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end
      else
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Left to origR.Right-1 do
           begin
             dato2:=linea[x];
             if dato2<>0 then// 0=transparente
               word(conta^):=TablaIndexadaDeColores[dato2];
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
    SuperficieRender.unlock(@Rectangulo_Origen);
  end;
end;

procedure BltFx(const DestR:Trect;origen:Tbitmap;const OrigR:Trect;colorFx:Tcolor;estilo:TFxAlpha);
var anchoFaltante:integer;
    x,y,r,g,b,ror,rog,rob:dword;
    conta:pointer;
    linea:Plinea8bits;
    dato:dword;
    reflejado:bytebool;
begin
  if SuperficieRender.lock(@Rectangulo_Origen,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    inc(integer(conta),(DestR.left+ancho_dd*DestR.top) shl 1);
    anchoFaltante:=(ancho_dd+origR.Left-origR.Right) shl 1;
    reflejado:=bytebool(ord(estilo) and Msk_FX_Reflejo);
    estilo:=TFxAlpha(ord(estilo) and Msk_FX_Estilo);
    r:=colorFX and $FF;
    g:=(colorFX shr 8) and $FF;
    b:=(colorFX shr 16){ and $FF};
    //nota por optimización tcpu reflejado es casi igual a normal, excepto el ciclo:
    if reflejado then
      case estilo of
      FxPlano:
      begin
        r:=r shl desRojoPl;
        g:=g shl desVerdePl;
        b:=b shr 3;
        for y:=origR.top to origR.Bottom-1 do
        begin
          linea:=origen.ScanLine[y];
          for x:=origR.Right-1 downto origR.Left do
          begin
            dato:=linea[x];
            if dato>3 then
            begin
              ror:=word(conta^);
              rob:=(ror and mskAzul);
              rog:=(ror and mskVerde);
              word(conta^):=(
                ( rob+(b-rob)*dato shr 8)or
                ((rog+(g-rog)*dato shr 8)and mskVerde) or
                ((ror+(r-ror)*dato shr 8)and mskRojo));
            end;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
        end;
      end;
      FxGradiente:
      begin
        for x:=0 to 255 do
        begin
          TablaR[x]:=(r*x) shr desRojo;
          TablaG[x]:=(g*x) shr 8;
          TablaB[x]:=(b*x) shr 8;
        end;
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Right-1 downto origR.Left do
           begin
             dato:=linea[x];//Nivel de Brillo
             if dato>3 then
             begin
               ror:=word(conta^);
               rob:=byte(ror shl 3);
               rog:=word(ror shl desVerde);
               word(conta^):=
                 ((rob+((TablaB[dato]-rob)*dato{p} shr 8)) shr 3) or
                 (((rog+(TablaG[dato]-(rog shr 8))*dato{p}) shr desVerde) and mskVerde) or
                 ((ror+(TablaR[dato]-(ror shr 8))*dato{p}) and mskRojo);
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      FxSumaSaturada:
      begin
        for x:=0 to 255 do
        begin
          y:=x+1;
          TablaR[x]:=(r*y) shr 8;
          TablaG[x]:=(g*y) shr 8;
          TablaB[x]:=(b*y) shr 11;//incluido "shr 3"
        end;
        //Nota: TablaG,TablaR DEBEN contener valores 0..255
        //TablaB DEBE estar en 0..31
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Right-1 downto origR.Left do
           begin
             dato:=linea[x];
             if dato>3 then
             begin
               ror:=word(conta^);
               word(conta^):=
                 SumaSatR[(ror shr desRojoPl)+TablaR[dato]] or
                 SumaSatG[byte(ror shr desVerdePl)+TablaG[dato]] or
                 SumaSatB[(ror and mskAzul)+TablaB[dato]];
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      FxSumaSaturadaColor:
      begin
        for x:=0 to 255 do
        begin
          y:=255-x;
          TablaR[x]:=QuitarNegativos(r-y);
          TablaG[x]:=QuitarNegativos(g-y);
          TablaB[x]:=QuitarNegativos(b-y) shr 3;
        end;
        //Nota: TablaG,TablaR DEBEN contener valores 0..255
        //TablaB DEBE estar en 0..31
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
          linea:=origen.ScanLine[y];
          for x:=origR.Right-1 downto origR.Left do
          begin
            dato:=linea[x];
            if dato>3 then
            begin
              ror:=word(conta^);
              word(conta^):=
                SumaSatR[(ror shr desRojoPl)+TablaR[dato]] or
                SumaSatG[byte(ror shr desVerdePl)+TablaG[dato]] or
                SumaSatB[(ror and mskAzul)+TablaB[dato]];
            end;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
        end;
      end;
      FxColorido:{TODO: Optimizar estos fx, precalculando en el constructor sus tablas de optimizacion}
      begin
        dec(r,255);
        dec(g,255);
        dec(b,255);
        for x:=0 to 255 do
        begin
          TablaR[x]:=QuitarNegativos(r+x);
          TablaG[x]:=QuitarNegativos(g+x);
          TablaB[x]:=QuitarNegativos(b+x);
        end;
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Right-1 downto origR.Left do
           begin
             dato:=linea[x];
             if dato>3 then
             begin
               ror:=word(conta^);
               rob:=byte(ror shl 3);
               rog:=word(ror shl desVerde);
               word(conta^):=
                 ((rob+(TablaB[dato]-rob)*dato shr 8) shr 3) or
                 (((rog+(TablaG[dato]-(rog shr 8))*dato) shr desVerde) and mskVerde) or
                 ((ror+(TablaR[dato]-(ror shr 8))*dato) and mskRojo);
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      FxExtraColorido:
      begin
        //dato2=el mayor
        if r>g then
          if r>b then
            dato:=r
          else
            dato:=b
        else
          if g>b then
            dato:=g
          else
            dato:=b;
        //rojo --------------------------
        if dato=r then
        begin
          dec(r,96);
          for x:=0 to 255 do
            TablaR[x]:=IntegerAByte(r+x);
        end
        else
        begin
          dec(r,255);
          for x:=0 to 255 do
            TablaR[x]:=QuitarNegativos(r+x)
        end;
        //Verde ----------
        if dato=g then
        begin
          dec(g,96);
          for x:=0 to 255 do
            TablaG[x]:=IntegerAByte(g+x);
        end
        else
        begin
          dec(g,255);
          for x:=0 to 255 do
            TablaG[x]:=QuitarNegativos(g+x);
        end;
        //Azul ----------
        if dato=b then
        begin
          dec(b,96);
          for x:=0 to 255 do
            TablaB[x]:=IntegerAByte(b+x);
        end
        else
        begin
          dec(b,255);
          for x:=0 to 255 do
            TablaB[x]:=QuitarNegativos(b+x);
        end;
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Right-1 downto origR.Left do
           begin
             dato:=linea[x];
             if dato>3 then
             begin
               ror:=word(conta^);
               rob:=byte(ror shl 3);
               rog:=word(ror shl desVerde);
               word(conta^):=
                 ((rob+(TablaB[dato]-rob)*dato shr 8) shr 3) or
                 (((rog+(TablaG[dato]-(rog shr 8))*dato) shr desVerde) and mskVerde) or
                 ((ror+(TablaR[dato]-(ror shr 8))*dato) and mskRojo);
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      end//case
    else
      case estilo of
      FxPlano:
      begin
        r:=r shl desRojoPl;
        g:=g shl desVerdePl;
        b:=b shr 3;
        for y:=origR.top to origR.Bottom-1 do
        begin
          linea:=origen.ScanLine[y];
          for x:=origR.Left to origR.Right-1 do
          begin
            dato:=linea[x];
            if dato>3 then
            begin
              ror:=word(conta^);
              rob:=(ror and mskAzul);
              rog:=(ror and mskVerde);
              word(conta^):=(
                ( rob+(b-rob)*dato shr 8)or
                ((rog+(g-rog)*dato shr 8)and mskVerde) or
                ((ror+(r-ror)*dato shr 8)and mskRojo));
            end;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
        end;
      end;
      FxGradiente:
      begin
        for x:=0 to 255 do
        begin
          TablaR[x]:=(r*x) shr desRojo;
          TablaG[x]:=(g*x) shr 8;
          TablaB[x]:=(b*x) shr 8;
        end;
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Left to origR.Right-1 do
           begin
             dato:=linea[x];//Nivel de Brillo
             if dato>3 then
             begin
               ror:=word(conta^);
               rob:=byte(ror shl 3);
               rog:=word(ror shl desVerde);
               word(conta^):=
                 ((rob+((TablaB[dato]-rob)*dato{p} shr 8)) shr 3) or
                 (((rog+(TablaG[dato]-(rog shr 8))*dato{p}) shr desVerde) and mskVerde) or
                 ((ror+(TablaR[dato]-(ror shr 8))*dato{p}) and mskRojo);
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      FxSumaSaturada:
      begin
        for x:=0 to 255 do
        begin
          y:=x+1;
          TablaR[x]:=(r*y) shr 8;
          TablaG[x]:=(g*y) shr 8;
          TablaB[x]:=(b*y) shr 11;//incluido "shr 3"
        end;
        //Nota: TablaG,TablaR DEBEN contener valores 0..255
        //TablaB DEBE estar en 0..31
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Left to origR.Right-1 do
           begin
             dato:=linea[x];
             if dato>3 then
             begin
               ror:=word(conta^);
               word(conta^):=
                 SumaSatR[(ror shr desRojoPl)+TablaR[dato]] or
                 SumaSatG[byte(ror shr desVerdePl)+TablaG[dato]] or
                 SumaSatB[(ror and mskAzul)+TablaB[dato]];
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      FxSumaSaturadaColor:
      begin
        for x:=0 to 255 do
        begin
          y:=255-x;
          TablaR[x]:=QuitarNegativos(r-y);
          TablaG[x]:=QuitarNegativos(g-y);
          TablaB[x]:=QuitarNegativos(b-y) shr 3;
        end;
        //Nota: TablaG,TablaR DEBEN contener valores 0..255
        //TablaB DEBE estar en 0..31
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
          linea:=origen.ScanLine[y];
          for x:=origR.Left to origR.Right-1 do
          begin
            dato:=linea[x];
            if dato>3 then
            begin
              ror:=word(conta^);
              word(conta^):=
                SumaSatR[(ror shr desRojoPl)+TablaR[dato]] or
                SumaSatG[byte(ror shr desVerdePl)+TablaG[dato]] or
                SumaSatB[(ror and mskAzul)+TablaB[dato]];
            end;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
        end;
      end;
      FxColorido:{TODO: Optimizar estos fx, precalculando en el constructor sus tablas de optimizacion}
      begin
        dec(r,255);
        dec(g,255);
        dec(b,255);
        for x:=0 to 255 do
        begin
          TablaR[x]:=QuitarNegativos(r+x);
          TablaG[x]:=QuitarNegativos(g+x);
          TablaB[x]:=QuitarNegativos(b+x);
        end;
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Left to origR.Right-1 do
           begin
             dato:=linea[x];
             if dato>3 then
             begin
               ror:=word(conta^);
               rob:=byte(ror shl 3);
               rog:=word(ror shl desVerde);
               word(conta^):=
                 ((rob+(TablaB[dato]-rob)*dato shr 8) shr 3) or
                 (((rog+(TablaG[dato]-(rog shr 8))*dato) shr desVerde) and mskVerde) or
                 ((ror+(TablaR[dato]-(ror shr 8))*dato) and mskRojo);
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      FxExtraColorido:
      begin
        //dato2=el mayor
        if r>g then
          if r>b then
            dato:=r
          else
            dato:=b
        else
          if g>b then
            dato:=g
          else
            dato:=b;
        //rojo --------------------------
        if dato=r then
        begin
          dec(r,96);
          for x:=0 to 255 do
            TablaR[x]:=IntegerAByte(r+x);
        end
        else
        begin
          dec(r,255);
          for x:=0 to 255 do
            TablaR[x]:=QuitarNegativos(r+x)
        end;
        //Verde ----------
        if dato=g then
        begin
          dec(g,96);
          for x:=0 to 255 do
            TablaG[x]:=IntegerAByte(g+x);
        end
        else
        begin
          dec(g,255);
          for x:=0 to 255 do
            TablaG[x]:=QuitarNegativos(g+x);
        end;
        //Azul ----------
        if dato=b then
        begin
          dec(b,96);
          for x:=0 to 255 do
            TablaB[x]:=IntegerAByte(b+x);
        end
        else
        begin
          dec(b,255);
          for x:=0 to 255 do
            TablaB[x]:=QuitarNegativos(b+x);
        end;
        //Aplicar el fx.
        for y:=origR.top to origR.Bottom-1 do
        begin
           linea:=origen.ScanLine[y];
           for x:=origR.Left to origR.Right-1 do
           begin
             dato:=linea[x];
             if dato>3 then
             begin
               ror:=word(conta^);
               rob:=byte(ror shl 3);
               rog:=word(ror shl desVerde);
               word(conta^):=
                 ((rob+(TablaB[dato]-rob)*dato shr 8) shr 3) or
                 (((rog+(TablaG[dato]-(rog shr 8))*dato) shr desVerde) and mskVerde) or
                 ((ror+(TablaR[dato]-(ror shr 8))*dato) and mskRojo);
             end;
             inc(integer(conta),2);
           end;
           inc(integer(conta),anchoFaltante);
        end;
      end;
      end;//case
    SuperficieRender.unlock(@Rectangulo_Origen);
  end;
end;

procedure BltZonaFrontera(const DestR:Trect);
var limite,N_lpitch:integer;
    conta:pointer;
begin
//SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    N_lpitch:=DescSuperficieLockUnlock.lPitch;
    if N_lpitch>=6 then
    begin
      limite:=integer(conta)+(DestR.bottom-DestR.top)*N_lpitch;
      N_lpitch:=N_lpitch-4;
      while integer(conta)<limite do
      begin
        longword(conta^):=$FFFFFFFF;
        inc(integer(conta),4);
        word(conta^):=$FFFF;
        inc(integer(conta),N_lpitch);
      end;
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltMarcoDialogo(const DestR:Trect);
//No controla límites!!
//Ancho y alto >= 8 !!!
var y,anchoFaltante,alto,anchoBytes,limitex,lpitchDoble:integer;
    conta:pointer;
begin
//SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    lpitchDoble:=DescSuperficieLockUnlock.lPitch shl 1;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    alto:=(DestR.bottom-DestR.top)-1;
    anchoBytes:=(DestR.right-DestR.left) shl 1;
    //Margen superior
    inc(integer(conta),DescSuperficieLockUnlock.lPitch);
    fillchar(conta^,anchoBytes,$0);
    inc(integer(conta),lpitchDoble);
    fillchar(conta^,anchoBytes,$0);
    inc(integer(conta),DescSuperficieLockUnlock.lPitch);
    for y:=4 to alto-4 do
    begin
      //Margen izquierdo
      inc(integer(conta),2);
      word(conta^):=$0;
      inc(integer(conta),4);
      word(conta^):=$0;
      inc(integer(conta),2);
      //Interior
      limitex:=integer(conta)+anchoBytes-16;
      while integer(conta)<limitex do
      begin
        word(conta^):=word(conta^) and mskAzul;
        inc(integer(conta),2);
      end;
      //Margen derecho
      word(conta^):=$0;
      inc(integer(conta),4);
      word(conta^):=$0;
      inc(integer(conta),4);
      //siguiente fila
      inc(integer(conta),anchoFaltante);
    end;
    fillchar(conta^,anchoBytes,$0);
    inc(integer(conta),lpitchDoble);
    fillchar(conta^,anchoBytes,$0);
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure AplicarFXAmbientalRayo;
var
    //inicioy,finy,j,limiteX,
    contadorY,anchoFaltante,AnchoBytesX,LongitudTotalBytes:integer;
    posicionD:pointer;
    res:hresult;
    //MascaraSaturacion,BitsAltos:word;
begin
  res:=SuperficieRender.lock(@Rectangulo_Origen,DescSuperficieLockUnlock,DDLOCK_WAIT,0);
  if res=DD_OK then
  begin
    With DescSuperficieLockUnlock,Limites_lienzo do
    begin
      posicionD:=pointer(integer(lpSurface)+(left+top*Ancho_dd) shl 1);
      anchoFaltante:=(Ancho_dd-(right-left)) shl 1;
      AnchoBytesX:=(right-left) shl 1;
      LongitudTotalBytes:=(bottom-top)*(anchoBytesX+anchoFaltante);
{      inicioy:=top;
      finy:=bottom-1;}
    end;
    asm
      //ebx = posicionD
      //edi = limiteX
      //ecx = pixel del fondo
      //eax = bitsAltos
      //esi = mskBitsAltos
    push ebx
    push edi
    push esi
      mov ebx,posicionD
      mov esi,ebx
      add esi,LongitudTotalBytes
      mov contadorY,esi
      mov esi,mskBitsAltos
      jmp @whileY
      @cicloY:
        mov edi,ebx
        add edi,AnchoBytesX
        jmp @whileX
        @cicloX:
          movzx ecx,word ptr[ebx] //ecx tiene el pixel
          mov eax,ecx
          add ecx,ecx
          and eax,esi  //eax tiene los bits altos
          mov edx,eax
          shr edx,1
          or eax,edx
          mov edx,eax
          shr edx,2
          or ecx,eax
          or ecx,edx
          mov word ptr[ebx],cx
          add ebx,2
        @whileX:
        cmp edi,ebx
        jg @cicloX
        add ebx,AnchoFaltante
      @whileY:
      cmp contadorY,ebx
      jg @cicloY
    pop esi
    pop edi
    pop ebx
    end;
    SuperficieRender.unlock(@Rectangulo_Origen);
  end;
end;

procedure AplicarNieveAmbiental(contadorExterno:integer;intensidad:byte);
const
  vlAzulNieve=31;
  vlVerdeNieve=1984;//31*64
  vlRojoNieve=63488;//31*2048
var base,x,y,posicionY,ContadorX:integer;
    ROrigen,RDestino:Trect;
    procedure BltFxCopoDeNieve(const DestR,OrigR:Trect);
    var anchoFaltante,anchoFaltanteOrigen,anchox,limiteX,limiteY:integer;
        dato,dato2:dword;
        conta,contaOrigen:pointer;
    begin
    //SuperficieRender
      if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
      begin
        conta:=DescSuperficieLockUnlock.lpSurface;
        anchox:=(DestR.Right-DestR.Left);
        anchoFaltanteOrigen:=AnchoLluvia-anchox;
        anchoFaltante:=DescSuperficieLockUnlock.lPitch-(anchox shl 1);
        contaOrigen:=pointer(integer(ImagenNieve)+OrigR.Top*anchoLluvia+OrigR.Left);
        limitey:=integer(contaOrigen)+(DestR.bottom-DestR.top)*anchoLluvia;
        while integer(contaOrigen)<limiteY do
        begin
          limitex:=integer(contaOrigen)+anchoX;
          while integer(contaOrigen)<limiteX do
          begin
            dato:=TablaB[byte(contaOrigen^)];
            if dato<60 then
            begin
              dato2:=word(conta^);
              word(conta^):=(
                (vlAzulNieve+((dato2 and mskAzul){b}-vlAzulNieve)*dato shr 6) or
                ((vlVerdeNieve+((dato2 and mskVerde){g}-vlVerdeNieve)*dato shr 6)and mskVerde) or
                ((vlRojoNieve+((dato2-vlRojoNieve)shr 6) *dato) and mskRojo));
            end;
            inc(integer(conta),2);
            inc(integer(contaOrigen));
          end;
          inc(integer(contaOrigen),anchoFaltanteOrigen);
          inc(integer(conta),anchoFaltante);
        end;
        SuperficieRender.unlock(@DestR);
      end;
    end;
begin
  posicionY:=(contadorExterno and $3) shl log2altoNieve;
  ContadorX:=contadorExterno and $1F * 5;
  contadorExterno:=contadorExterno and $F shl 3;

  base:=(200-intensidad);
  if base<0 then
    base:=0
  else
    base:=base shr 2;//shr 2, a shr 3 por visibilidad
  for x:=0 to 63 do
  begin
    y:=x+base;
    if y>64 then
      TablaB[x]:=64
    else
      TablaB[x]:=y;
  end;
  for y:=0 to 5 do
    for x:=0 to 8 do//movimiento especial
    begin
      ROrigen:=rect(0,posicionY,anchoLluvia,posicionY+altoNieve);
      RDestino:=rect(x*anchoLluvia-ContadorX,(y shl log2altoNieve)-altoNieve+contadorExterno,
        x*anchoLluvia+anchoLluvia-ContadorX,(y shl log2altoNieve)+contadorExterno);
      if EstaEnPantalla(rDestino,rOrigen,false) then
        BltFxCopoDeNieve(RDestino,ROrigen);
    end;
end;

procedure AplicarLluviaAmbiental(contadorExterno:integer;intensidad:byte);
  //bits:  565565
  //colors: g b r
const
  vlAzulNiebla=24;
  vlVerdeNiebla=24*64;
  vlRojoNiebla=24*2048;
  vlMagentaNiebla=vlAzulNiebla+vlRojoNiebla;

var base,x,y,posicionY,ContadorX:integer;
    ROrigen,RDestino:Trect;
    procedure BltFxLluvia(const DestR,OrigR:Trect);
    var anchoFaltante,anchoFaltanteOrigen,anchox,limiteY,baseFx:integer;
        conta,contaOrigen:pointer;
    begin
      // La imagen de lluvia debe contener bytes en el rango 0..31
      if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
      begin
        conta:=DescSuperficieLockUnlock.lpSurface;
        anchox:=(DestR.Right-DestR.Left);
        anchoFaltanteOrigen:=AnchoLluvia-anchox;
        anchoFaltante:=DescSuperficieLockUnlock.lPitch-(anchox shl 1);
        contaOrigen:=pointer(integer(ImagenLluvia)+OrigR.Top*anchoLluvia+OrigR.Left);
        limitey:=integer(contaOrigen)+(DestR.bottom-DestR.top)*anchoLluvia;
        baseFx:=base;
        asm
        push ebx
        push edi
        push esi

        mov edi,contaOrigen
        mov esi,conta

        jmp @whileY
        @cicloY:
          mov ecx,edi
          add ecx,anchoX

          jmp @whileX
          @cicloX:
            movzx ebx,byte ptr[edi]
            add ebx,baseFx
            cmp ebx,30
            jae @done
              movzx eax,word ptr[esi]
              mov edx,eax

              and edx,mskVerde
              sub edx,vlVerdeNiebla
              imul edx,ebx
              shr edx,5
              and edx,mskVerde
              add edx,vlVerdeNiebla

              and eax,mskMagenta
              sub eax,vlMagentaNiebla
              imul eax,ebx
              shr eax,5
              and eax,mskMagenta
              add eax,vlMagentaNiebla

              or eax,edx
              mov word ptr[esi],ax

            @done:
            add esi,2
            inc edi

          @whileX:
            cmp edi,ecx
            jb @cicloX

          add esi,anchoFaltante
          add edi,anchoFaltanteOrigen

        @whileY:
          cmp edi,limiteY
          jb @cicloY

        pop esi
        pop edi
        pop ebx
        end;

        SuperficieRender.unlock(@DestR);
      end;
    end;
begin
  posicionY:=(contadorExterno and $3) shl log2altoLluvia;
  ContadorX:=contadorExterno and $1F * 5;
  contadorExterno:=contadorExterno and $F shl 4;
  base:=(200-intensidad);
  if base<0 then base:=0 else base:=base shr 3;
  for y:=0 to 3 do
    for x:=0 to 8 do//movimiento especial
    begin
      ROrigen:=rect(0,posicionY,anchoLluvia,posicionY+altoLluvia);
      RDestino:=rect(x*anchoLluvia-anchoLluvia+ContadorX,(y shl log2altoLluvia)-altoLluvia+contadorExterno,
        x*anchoLluvia+ContadorX,(y shl log2altoLluvia)+contadorExterno);
      if EstaEnPantalla(rDestino,rOrigen,false) then
        BltFxLluvia(RDestino,ROrigen);
    end;
end;

procedure BltTrans(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
var x,y,anchoFaltante,anchoFaltanteOrig,ancho:integer;
    conta,contaOrig:pointer;
begin
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    if SuperficieDD.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      if espejo then
      begin
        contaOrig:=Pointer(integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1);
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch+(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface);
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            if word(contaOrig^)<>0 then
              word(conta^):=((word(contaOrig^) and mskTrans)+(word(conta^) and mskTrans)) shr 1;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end
      else
      begin
        contaOrig:=DescSuperficieLockUnlock.lpSurface;
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1;
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)<x do
          begin
            if word(contaOrig^)<>0 then
              word(conta^):=((word(contaOrig^) and mskTrans)+(word(conta^) and mskTrans)) shr 1;
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end;
      SuperficieDD.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltAntiAlisado(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
var x,y,anchoFaltante,anchoFaltanteOrig,ancho,alto:integer;
    conta,contaOrig:pointer;
    PixelImagen:word;
    PixelFondo:word;
    PixelFondoAnterior:word;
    PixelAnterior:word;
begin
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    //SuperficieTransparente
    if SuperficieDD.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      alto:=origR.Bottom-origR.top;
      ancho:=DescSuperficieLockUnlock.lPitch;
      PixelFondoAnterior:=0;
      if espejo then
      begin // VISTA EN ESPEJO *******
        contaOrig:=Pointer(integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1);
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch+(origR.Right-origR.Left) shl 1;
        x:=integer(DescSuperficieLockUnlock.lpSurface);
        //Primera linea
        if alto>0 then
        begin
          PixelAnterior:=0;
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            PixelImagen:=word(contaOrig^);
            PixelFondo:=word(conta^);
            if PixelImagen<>0 then
              if (PixelAnterior=0) then
                word(conta^):=((PixelImagen and mskTrans)+(PixelFondo and mskTrans)) shr 1
              else
                word(conta^):=PixelImagen
            else
              if PixelAnterior<>0 then
                word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
            PixelFondoAnterior:=PixelFondo;
            PixelAnterior:=PixelImagen;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
        for y:=0 to alto-3 do
        begin
          PixelAnterior:=0;
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            PixelImagen:=word(contaOrig^);
            PixelFondo:=word(conta^);
            if PixelImagen<>0 then
              if (PixelAnterior=0) and ((word(pointer(integer(contaOrig)+ancho)^)=0) or (word(pointer(integer(contaOrig)-ancho)^)=0)) then
                word(conta^):=((PixelImagen and mskTrans)+(PixelFondo and mskTrans)) shr 1
              else
                word(conta^):=PixelImagen
            else
              if (PixelAnterior<>0) and ((word(pointer(integer(contaOrig)+2+ancho)^)=0) or (word(pointer(integer(contaOrig)+2-ancho)^)=0)) then
                word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
            PixelFondoAnterior:=PixelFondo;
            PixelAnterior:=PixelImagen;
            inc(integer(conta),2);
          end;
          //ultimo pixel
          if (PixelAnterior<>0) and ((word(pointer(integer(contaOrig)+ancho)^)=0) or (word(pointer(integer(contaOrig)-ancho)^)=0)) then
            word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
        //ultima linea
        if alto>1 then
        begin
          PixelAnterior:=0;
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            PixelImagen:=word(contaOrig^);
            PixelFondo:=word(conta^);
            if PixelImagen<>0 then
              if (PixelAnterior=0) then
                word(conta^):=((PixelImagen and mskTrans)+(PixelFondo and mskTrans)) shr 1
              else
                word(conta^):=PixelImagen
            else
              if PixelAnterior<>0 then
                word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
            PixelFondoAnterior:=PixelFondo;
            PixelAnterior:=PixelImagen;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
        end;
      end
      else// VISTA NORMAL ******
      begin
        contaOrig:=DescSuperficieLockUnlock.lpSurface;
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
        x:=integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1;
        //Primera linea
        if alto>0 then
        begin
          PixelAnterior:=0;
          while integer(contaOrig)<x do
          begin
            PixelImagen:=word(contaOrig^);
            PixelFondo:=word(conta^);
            if PixelImagen<>0 then
              if PixelAnterior=0 then
                word(conta^):=((PixelImagen and mskTrans)+(PixelFondo and mskTrans)) shr 1
              else
                word(conta^):=PixelImagen
            else
              if PixelAnterior<>0 then
                word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
            PixelFondoAnterior:=PixelFondo;
            PixelAnterior:=PixelImagen;
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
        for y:=0 to alto-3 do
        begin
          PixelAnterior:=0;
          while integer(contaOrig)<x do
          begin
            PixelImagen:=word(contaOrig^);
            PixelFondo:=word(conta^);
            if PixelImagen<>0 then
              if (PixelAnterior=0) and ((word(pointer(integer(contaOrig)+ancho)^)=0) or (word(pointer(integer(contaOrig)-ancho)^)=0))  then
                word(conta^):=((PixelImagen and mskTrans)+(PixelFondo and mskTrans)) shr 1
              else
                word(conta^):=PixelImagen
            else
              if (PixelAnterior<>0) and ((word(pointer(integer(contaOrig)-2+ancho)^)=0) or (word(pointer(integer(contaOrig)-2-ancho)^)=0)) then
                word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
            PixelFondoAnterior:=PixelFondo;
            PixelAnterior:=PixelImagen;
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          //ultimo pixel
          if (PixelAnterior<>0) and ((word(pointer(integer(contaOrig)-2+ancho)^)=0) or (word(pointer(integer(contaOrig)-2-ancho)^)=0)) then
            word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
        //ultima linea
        if alto>1 then
        begin
          PixelAnterior:=0;
          while integer(contaOrig)<x do
          begin
            PixelImagen:=word(contaOrig^);
            PixelFondo:=word(conta^);
            if PixelImagen<>0 then
              if PixelAnterior=0 then
                word(conta^):=((PixelImagen and mskTrans)+(PixelFondo and mskTrans)) shr 1
              else
                word(conta^):=PixelImagen
            else
              if PixelAnterior<>0 then
                word(pointer(integer(conta)-2)^):=((PixelFondoAnterior and mskTrans)+(pixelAnterior and mskTrans)) shr 1;
            PixelFondoAnterior:=PixelFondo;
            PixelAnterior:=PixelImagen;
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
        end;
      end;
      SuperficieDD.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltAlpha(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool;alpha:byte);
var x,y,anchoFaltante,anchoFaltanteOrig,ancho:integer;
    conta,contaOrig:pointer;
    ColorO,ColorD:integer;
begin
  if alpha=255 then
  begin//Totalmente opaco
    BltMejorado(DestR,SuperficieDD,OrigR,espejo);
    exit;
  end;
  alpha:=(alpha shr 5);
  if alpha=0 then exit;//totalmente transparente ;)
  if alpha=4 then//transparente al 50%
  begin
    BltTrans(DestR,SuperficieDD,OrigR,espejo);
    exit;
  end;
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    //SuperficieTransparente
    if SuperficieDD.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      if espejo then
      begin
        contaOrig:=Pointer(integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1);
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch+(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface);
        case alpha of
          7:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)>x do
            begin
              dec(integer(contaOrig),2);
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          6:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)>x do
            begin
              dec(integer(contaOrig),2);
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          5:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)>x do
            begin
              dec(integer(contaOrig),2);
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorO+colorD) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          3:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)>x do
            begin
              dec(integer(contaOrig),2);
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorO+colorD) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          2:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)>x do
            begin
              dec(integer(contaOrig),2);
              if word(contaOrig^)<>0 then
              begin
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorD+(word(contaOrig^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          1:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)>x do
            begin
              dec(integer(contaOrig),2);
              if word(contaOrig^)<>0 then
              begin
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorD+(word(contaOrig^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
        end;
      end
      else
      begin
        contaOrig:=DescSuperficieLockUnlock.lpSurface;
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1;
        case alpha of
          7:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          6:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          5:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorO+colorD) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          4:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
                word(conta^):=((word(contaOrig^) and mskTrans)+
                  (word(conta^) and mskTrans)) shr 1;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          3:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
              begin
                colorO:=word(contaOrig^) and mskTrans;
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorO+colorD) shr 1;
                word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          2:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
              begin
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorD+(word(contaOrig^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
          1:for y:=origR.top to origR.Bottom-1 do
          begin
            while integer(contaOrig)<x do
            begin
              if word(contaOrig^)<>0 then
              begin
                colorD:=word(conta^) and mskTrans;
                word(conta^):=(colorD+(word(contaOrig^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
                word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              end;
              inc(integer(contaOrig),2);
              inc(integer(conta),2);
            end;
            inc(integer(conta),anchoFaltante);
            inc(integer(contaOrig),anchoFaltanteOrig);
            inc(x,ancho);
          end;
        end;
      end;
      SuperficieDD.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltFxColor(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool;Color:integer);
var x,y,anchoFaltante,anchoFaltanteOrig,ancho:integer;
    conta,contaOrig:pointer;
    pixelImagen:integer;
begin
  Color:=ReducirBitsColor(color) and mskTrans;
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    //SuperficieTransparente
    if SuperficieDD.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      if espejo then
      begin
        contaOrig:=Pointer(integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1);
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch+(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface);
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            PixelImagen:=word(contaOrig^);
            if PixelImagen<>0 then
              //word(conta^):=((PixelImagen and mskTrans)+Color) shr 1;//50% color
            begin
              PixelImagen:=PixelImagen and mskTrans;
              word(conta^):=((((PixelImagen+Color) shr 1) and mskTrans)+PixelImagen) shr 1;//25% color
            end;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end
      else
      begin
        contaOrig:=DescSuperficieLockUnlock.lpSurface;
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1;
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)<x do
          begin
            PixelImagen:=word(contaOrig^);
            if PixelImagen<>0 then
            begin
              PixelImagen:=PixelImagen and mskTrans;
              word(conta^):=((((PixelImagen+Color) shr 1) and mskTrans)+PixelImagen) shr 1;//25% color
            end;
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end;
      SuperficieDD.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltFxMascara(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool;Color:integer);
var x,y,anchoFaltante,anchoFaltanteOrig,ancho{,ColorComplemento}:integer;
    conta,contaOrig:pointer;
begin
  Color:=ReducirBitsColor(color);
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    //SuperficieTransparente
    if SuperficieDD.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      if espejo then
      begin
        contaOrig:=Pointer(integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1);
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch+(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface);
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            if word(contaOrig^)<>0 then
              word(conta^):=word(contaOrig^) and Color;
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end
      else
      begin
        contaOrig:=DescSuperficieLockUnlock.lpSurface;
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1;
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)<x do
          begin
            if word(contaOrig^)<>0 then
              word(conta^):=word(contaOrig^) and Color;
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end;
      SuperficieDD.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltTransFan(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
var x,y,anchoFaltante,anchoFaltanteOrig,ancho:integer;
    conta,contaOrig:pointer;
    PixelImagen:word;
begin
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    //SuperficieTransparente
    if SuperficieDD.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      if espejo then
      begin
        contaOrig:=Pointer(integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1);
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch+(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface);
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)>x do
          begin
            dec(integer(contaOrig),2);
            PixelImagen:=word(contaOrig^);
            if PixelImagen<>0 then
              word(conta^):=(((word(conta^) and mskTransAmarillo)+(PixelImagen and mskTransAmarillo)) shr 1) or
                SumaSatB[(PixelImagen and mskAzul)+(word(conta^) and mskAzul)+(PixelImagen and mskRojo) shr desRojo5bits];
            inc(integer(conta),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end
      else
      begin
        contaOrig:=DescSuperficieLockUnlock.lpSurface;
        anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
        ancho:=DescSuperficieLockUnlock.lPitch;
        x:=integer(DescSuperficieLockUnlock.lpSurface)+(origR.Right-origR.Left) shl 1;
        for y:=origR.top to origR.Bottom-1 do
        begin
          while integer(contaOrig)<x do
          begin
            PixelImagen:=word(contaOrig^);
            if PixelImagen<>0 then
              word(conta^):=(((word(conta^) and mskTransAmarillo)+(PixelImagen and mskTransAmarillo)) shr 1) or
                SumaSatB[(PixelImagen and mskAzul)+(word(conta^) and mskAzul)+(PixelImagen and mskRojo) shr desRojo5bits];
            inc(integer(conta),2);
            inc(integer(contaOrig),2);
          end;
          inc(integer(conta),anchoFaltante);
          inc(integer(contaOrig),anchoFaltanteOrig);
          inc(x,ancho);
        end;
      end;
      SuperficieDD.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltTileEspejoNoColorKey(const DestR:Trect;const OrigR:Trect);
begin
  SuperficieRender.Blt(@DestR,SuperficieTerreno,@OrigR,DDBLT_DDFX,@DDBLTFX_H)
end;

procedure BltMejorado(const DestR:Trect;SuperficieDD:IDirectDrawSurface7;const OrigR:Trect;espejo:bytebool);
begin
  if espejo then
    SuperficieRender.Blt(@destR,SuperficieDD,@origR,DDBLT_KEYSRC or DDBLT_DDFX,@DDBLTFX_H)
  else
    SuperficieRender.BltFast(destR.left,destR.top,SuperficieDD,@origR,DDBLTFAST_SRCCOLORKEY);
end;

procedure BltLiquido(const DestR:Trect;const OrigR:Trect;paleta,tipo:byte);
var y,anchoFaltante,anchoFaltanteOrig,ancho,limitx,limity,subPaleta:integer;
    conta,contaOrig:pointer;
begin
  //SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    ancho:=DestR.Right-DestR.Left;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(ancho shl 1);
    contaOrig:=pointer(integer(ImagenLiquido)+OrigR.Top*ANCHO_LIQUIDO+OrigR.Left);
    subPaleta:=paleta*64+tipo*256;
    anchoFaltanteOrig:=ANCHO_LIQUIDO-ancho;
    limity:=origR.Bottom-1;
    for y:=origR.top to limity do
    begin
      limitx:=integer(contaOrig)+ancho;
      while integer(contaOrig)<limitx do
      begin
        word(conta^):=Paleta_Liquidos[byte(contaOrig^) + subPaleta];
        inc(integer(conta),2);
        inc(integer(contaOrig));
      end;
      inc(integer(conta),anchoFaltante);
      inc(integer(contaOrig),anchoFaltanteOrig);
    end;

    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltAlphaLiquido(const DestR:Trect;const OrigR:Trect;nro_mezcla,paleta,tipo:byte);
var y,anchoFaltante,anchoFaltanteOrig,anchoFaltanteAlpha,alto,ancho,limiteX,d,subPaleta:integer;
    conta,contaOrig,contaAlpha:pointer;
    colorO,colorD,colorT:word;
begin
//SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
    contaOrig:=pointer(integer(ImagenLiquido)+OrigR.Top*ANCHO_LIQUIDO+OrigR.Left);
    ancho:=(origR.right-origR.left);
    anchoFaltanteOrig:=ANCHO_LIQUIDO-ancho;
    anchoFaltanteAlpha:=ancho_tile-Ancho;
    alto:=(origR.Bottom-origR.top)-1;
    contaAlpha:=pointer(integer(enlaceMosaico)+((origR.top {mod alto_tile} and $F)+
      (nro_mezcla*alto_tile))*ancho_tile+(origR.left mod ancho_tile));
    subPaleta:=paleta*64+tipo*256;
    for y:=0 to alto do
    begin
      limiteX:=integer(contaAlpha)+ancho;
      while integer(contaAlpha)<limiteX do
      begin
        d:=Paleta_Liquidos[byte(contaOrig^) + subPaleta];
        case byte(contaAlpha^) of
          0:word(conta^):=d;
          1:begin
            colorO:=d and mskTrans;
            colorT:=(colorO+(word(conta^) and mskTrans)) shr 1;
            colorT:=(colorO+(colorT and mskTrans)) shr 1;
            word(conta^):=(colorO+(colorT and mskTrans)) shr 1;
          end;
          2:begin
            colorO:=d and mskTrans;
            colorT:=(colorO+(word(conta^) and mskTrans)) shr 1;
            word(conta^):=(colorO+(colorT and mskTrans)) shr 1;
          end;
          3:begin
            colorO:=d and mskTrans;
            colorD:=word(conta^) and mskTrans;
            colorT:=(colorO+colorD) shr 1;
            colorT:=(colorD+(colorT and mskTrans)) shr 1;
            word(conta^):=(colorO+(colorT and mskTrans)) shr 1;
          end;
          4:word(conta^):=((d and mskTrans)+(word(conta^) and mskTrans)) shr 1;
          5:begin
            colorO:=d and mskTrans;
            colorD:=word(conta^) and mskTrans;
            colorT:=(colorO+colorD) shr 1;
            colorT:=(colorO+(colorT and mskTrans)) shr 1;
            word(conta^):=(colorD+(colorT and mskTrans)) shr 1;
          end;
          6:begin
            colorD:=word(conta^) and mskTrans;
            colorT:=(colorD+(d and mskTrans)) shr 1;
            word(conta^):=(colorD+(colorT and mskTrans)) shr 1;
          end;
          7:begin
            colorD:=word(conta^) and mskTrans;
            colorT:=(colorD+(d and mskTrans)) shr 1;
            colorT:=(colorD+(colorT and mskTrans)) shr 1;
            word(conta^):=(colorD+(colorT and mskTrans)) shr 1;
          end;
        end;
        inc(integer(conta),2);
        inc(integer(contaOrig));
        inc(integer(contaAlpha));
      end;
      inc(integer(conta),anchoFaltante);
      inc(integer(contaOrig),anchoFaltanteOrig);
      inc(integer(contaAlpha),anchoFaltanteAlpha)
    end;

    SuperficieRender.unlock(@DestR);
  end;
end;

procedure BltAlphaTile(const DestR:Trect;const OrigR:Trect;nro_mezcla:byte);
var y,anchoFaltante,anchoFaltanteOrig,anchoFaltanteAlpha,anchoY,anchoX,limiteX:integer;
    conta,contaOrig,contaAlpha:pointer;
    colorO,colorD:word;
begin
//SuperficieRender
  if SuperficieRender.lock(@DestR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch-(DestR.Right-DestR.Left) shl 1;
//SuperficieTransparente
    if SuperficieTerreno.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      contaOrig:=DescSuperficieLockUnlock.lpSurface;
      anchoFaltanteOrig:=DescSuperficieLockUnlock.lPitch-(origR.Right-origR.Left) shl 1;
      anchoX:=(origR.right-origR.left);
      anchoFaltanteAlpha:=ancho_tile-AnchoX;
      anchoY:=(origR.Bottom-origR.top)-1;
      contaAlpha:=pointer(integer(enlaceMosaico)+((origR.top {mod alto_tile} and $F)+
        (nro_mezcla*alto_tile))*ancho_tile+(origR.left mod ancho_tile));
      for y:=0 to anchoY do
      begin
        limiteX:=integer(contaAlpha)+anchoX;
        while integer(contaAlpha)<limiteX do
        begin
          case byte(contaAlpha^) of
            0:word(conta^):=word(contaOrig^);
            1:begin
              colorO:=word(contaOrig^) and mskTrans;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
            end;
            2:begin
              colorO:=word(contaOrig^) and mskTrans;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
            end;
            3:begin
              colorO:=word(contaOrig^) and mskTrans;
              colorD:=word(conta^) and mskTrans;
              word(conta^):=(colorO+colorD) shr 1;
              word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
            end;
            4:word(conta^):=((word(contaOrig^) and mskTrans)+
              (word(conta^) and mskTrans)) shr 1;
            5:begin
              colorO:=word(contaOrig^) and mskTrans;
              colorD:=word(conta^) and mskTrans;
              word(conta^):=(colorO+colorD) shr 1;
              word(conta^):=(colorO+(word(conta^) and mskTrans)) shr 1;
              word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
            end;
            6:begin
              colorD:=word(conta^) and mskTrans;
              word(conta^):=(colorD+(word(contaOrig^) and mskTrans)) shr 1;
              word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
            end;
            7:begin
              colorD:=word(conta^) and mskTrans;
              word(conta^):=(colorD+(word(contaOrig^) and mskTrans)) shr 1;
              word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
              word(conta^):=(colorD+(word(conta^) and mskTrans)) shr 1;
            end;
          end;
          inc(integer(conta),2);
          inc(integer(contaOrig),2);
          inc(integer(contaAlpha));
        end;
        inc(integer(conta),anchoFaltante);
        inc(integer(contaOrig),anchoFaltanteOrig);
        inc(integer(contaAlpha),anchoFaltanteAlpha)
      end;
      SuperficieTerreno.unlock(@OrigR);
    end;
    SuperficieRender.unlock(@DestR);
  end;
end;

function RestaurarResolucionInicial():boolean;
var pModo:^DEVMODEA;
begin
  result:=false;
  if not g_cambiadoAResolucionHd then exit;

  pModo:=nil;
  result:=0=ChangeDisplaySettingsA(pModo^,0);
  if result then g_cambiadoAResolucionHd:=false;
end;

function CambiarAResolucionHD():boolean;
var modo:DEVMODEA;
begin
  result:=false;
  if g_cambiadoAResolucionHd then exit;

  FillChar(modo, SizeOf(modo), 0);
  modo.dmSize:=SizeOf(modo);
  modo.dmPelsWidth:=1280;
  modo.dmPelsHeight:=720;
  modo.dmDisplayFrequency:=60;
  modo.dmFields:= DM_PELSWIDTH or DM_PELSHEIGHT or DM_DISPLAYFREQUENCY;
  result:=0=ChangeDisplaySettingsA(modo, CDS_FULLSCREEN);
  if result then g_cambiadoAResolucionHd:=true;
end;

procedure CopyTransMagenta(SupDest:IDirectDrawSurface7;DestX,DestY,Ancho,Alto:integer;Origen:Tbitmap;OrigX,OrigY:integer);
//Solo hacia superficieRender, origenx=0, origeny=0
//var HDCSuperficie:HDC;
var
    OrigR:Trect;
    conta:pointer;
    anchoFaltante,x,y,iniciox,finx:integer;
    linea:^Tlinea16bits;
begin
//SuperficieRender
  with OrigR do
  begin
    left:=DestX;
    top:=DestY;
    right:=DestX+Ancho;
    bottom:=DestY+Alto;
  end;
  if SupDest.lock(@OrigR,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
  begin
    conta:=DescSuperficieLockUnlock.lpSurface;
    anchoFaltante:=DescSuperficieLockUnlock.lPitch+(origR.Left-origR.Right) shl 1;
    iniciox:=OrigX;
    finx:=OrigX+Ancho-1;
    for y:=Origy to Origy+Alto-1 do
    begin
      linea:=origen.ScanLine[y];
      for x:=inicioX to finX do
      begin
        if linea[x]<>ClaveColMagenta then //magenta
          word(conta^):=linea[x];
        inc(integer(conta),2);
      end;
      inc(integer(conta),anchoFaltante);
    end;
    SupDest.unlock(@OrigR);
  end;
end;

function CrearDeGDD8bits(const filename:string):Tbitmap;
begin
  result:=Tbitmap.create;
  result.LoadFromFile(filename);
  result.pixelFormat:=pf8bit;
  result.HandleType:=bmDIB;
end;

function CrearBackBuffer16Bits(ancho,alto:integer):Tbitmap;
begin
  result:=Tbitmap.create;
  with result do
  begin
    PixelFormat:=pf16bit;
    HandleType:=bmDIB;
    Width:=ancho;
    Height:=alto;
  end;
end;

function CrearDeGDD(const filename:string):Tbitmap;
begin
  result:=Tbitmap.create;
  result.HandleType:=bmDib;//Device Independet Bitmap
  result.LoadFromFile(filename);
end;

function CrearDeJDD(const filename:string):Tbitmap;
var imgGDD:TJPEGImage;
begin
  imgGDD:=TJpegImage.create;
  imgGDD.LoadFromFile(filename);
  result:=CrearBackBuffer16Bits(imgGdd.width,imgGdd.height);
  result.Canvas.Draw(0,0,imgGdd);
  imgGDD.free;
end;

function CrearSuperficieDeBMP(var superficie:IDirectDrawSurface7;const filename:string):boolean;
var BitMapTemporal:Tbitmap;
begin
  BitMapTemporal:=Tbitmap.create;
  BitMapTemporal.LoadFromFile(filename);
  result:=CrearSuperficieOculta(superficie,BitMapTemporal.width,BitMapTemporal.Height,etNegro)=DD_OK;
  if result then CopiarCanvasASuperficie(superficie,0,0,BitMapTemporal.width,BitMapTemporal.Height,BitMapTemporal.canvas.handle,0,0);
  BitMapTemporal.free;
end;

function CrearSuperficieDeJDD(var superficie:IDirectDrawSurface7;const filename:string):boolean;
var BitMapTemporal:Tbitmap;
begin
  BitMapTemporal:=CrearDeJDD(filename);
  result:=CrearSuperficieOculta(superficie,BitMapTemporal.width,BitMapTemporal.Height,etNinguno)=DD_OK;
  if result then CopiarCanvasASuperficie(superficie,0,0,BitMapTemporal.width,BitMapTemporal.Height,BitMapTemporal.canvas.handle,0,0);
  BitMapTemporal.free;
end;

procedure CambiarAreaDibujable(zoom,PanelDerechoActivo:longbool);
begin
  if PanelDerechoActivo then
    if Zoom then
      Limites_Lienzo:=Area_Dibujable_TablaD_Zoom
    else
      Limites_Lienzo:=Area_Dibujable_TablaD
  else
    if Zoom then
      Limites_Lienzo:=Area_Dibujable_Zoom
    else
      Limites_Lienzo:=Rectangulo_Origen;
end;
// Imagen40x40 Iconos
//************************************************************

function QuitarSobreflujos32(const n:integer):byte; register;
// si n>31, n:=31.
// n=EAX
asm
  cmp eax,31
  jle @fin
  mov al,31
  @fin:
end;

function QuitarSobreflujos64(const n:integer):byte; register;
// si n>63, n:=63.
// n=EAX
asm
  cmp eax,63
  jle @fin
  mov al,63
  @fin:
end;

constructor TImagen40.create;
var i,x:integer;
begin
  inherited create;
  HandleType:=bmDIB;//Para editar sin problema
  PixelFormat:=pf16bit;
  width:=40;
  height:=40;
  for i:=0 to 127 do
  begin
    MC_resalte_1_128[i]:=QuitarSobreflujos32(round(sin(i/260*pi)*42));
    MC_resalte_2_128[i]:=QuitarSobreflujos32(round(sin(i/260*pi)*44)) shl 11;
    MC_resalte_3_128[i]:=QuitarSobreflujos64(i) shl 5;
  end;
  for i:=0 to 31 do
  begin
    MC_resalte_1_32[i]:=QuitarNegativos(i-9);
    MC_resalte_2_32[i]:=QuitarNegativos(i-9) shl 11;
  end;
  for i:=0 to 127 do//para grises:
  begin
    x:=round(i*0.625);
    if x>31 then x:=31;
    MC_Grises_128[i]:=x or (x shl 6) or (x shl 11);
  end;
  for i:=0 to 127 do//para grises:
  begin
    x:=round(i*0.4);
    if x>31 then x:=31;
    MC_BrilloGris_128[i]:=x shl 11;//rojo
    x:=round(x*0.9);
    MC_BrilloGris_128[i]:=MC_BrilloGris_128[i] or (x shl 6);//verde
    x:=round(x*0.6);
    MC_BrilloGris_128[i]:=MC_BrilloGris_128[i] or x;//azul
  end;
  with canvas do
  begin
    brush.style:=bsClear;
    font.color:=clWhite;
  end;
end;

procedure TImagen40.copiarImagen(x,y:integer;img:TBitmap;efecto:TBrilloFxObjeto);
type
  Tlinea=array[0..600] of word;
var linea:^Tlinea;
    i,j:integer;
    nivel:dword;
begin
  bitblt(canvas.handle,0,0,40,40,img.canvas.handle,x,y,SRCCOPY);
  case TBrilloFxObjeto(efecto) of
    bfxCongelado:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
        linea[i]:=MC_resalte_1_128[(linea[i] and mskAzul)+((linea[i] and mskVerde16) shr 5)]
          or (linea[i] and mskVerde16)
          or MC_resalte_2_32[linea[i] shr 11];
    end;
    bfxMagico:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
        linea[i]:=MC_resalte_1_128[(linea[i] and mskAzul)+((linea[i] and mskVerde16) shr 5)+(linea[i] shr 11)]
          or (linea[i] and mskVerde16)
          or MC_resalte_2_32[linea[i] shr 11];
    end;
    bfxMalvado:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
      begin
        Linea[i]:=MC_resalte_1_32[linea[i] and mskAzul] or
          (linea[i] and mskVerde16) or
          MC_resalte_2_128[linea[i] shr 11+(linea[i] and mskVerde16) shr 5+(linea[i] and mskAzul)]
      end;
    end;
    bfxVenenoso:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
      begin
        Linea[i]:=(linea[i] and mskAzul) or
                  MC_resalte_3_128[((linea[i] and mskVerde16) shr 5)+(linea[i] and mskAzul)] or
                  MC_resalte_2_32[linea[i] shr 11];
      end;
    end;
    bfxGris:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
        linea[i]:=MC_grises_128[(linea[i] and mskAzul)+(linea[i] shr 11)]
    end;
    bfxBrilloGrisDesactivado:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
      begin
        y:=40-abs(j-20);
        nivel:=y-abs(i-20);
        x:=(linea[i] and mskAzul)+(linea[i] shr 11)+nivel shr 1;
        linea[i]:=MC_BrilloGris_128[x];
      end;
    end;
    bfxBrilloGris:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      y:=40-abs(j-20);
      for i:=0 to 39 do
      begin
        nivel:=y-abs(i-20);
        x:=(linea[i] and mskAzul)+(linea[i] shr 11)+nivel;
        linea[i]:=MC_BrilloGris_128[x];
      end;
    end;
    bfxOscuro:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
        linea[i]:=((linea[i] and mskAzul) or ((linea[i] and $E780) shr 1)) shr 1
    end;
    bfxDesactivado:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
        if ((i+(j shl 1)) and $2)=0 then
          linea[i]:=(linea[i] and $F7DE) shr 1;
    end;
    bfxFuegoInterno:
    for j:=0 to 39 do
    begin
      linea:=scanline[j];
      for i:=0 to 39 do
      begin
        Linea[i]:=MC_resalte_2_128[linea[i] shr 10] or (linea[i] and mskVerde16) or
          MC_resalte_1_32[linea[i] and mskAzul];
      end;
    end;
  end;
end;

procedure TImagen40.copiarTransMagenta(img:TBitmap);
type
  Tlinea=array[0..0] of word;
var linea:^Tlinea;
    lineao:^Tlinea;
    i,j:integer;
begin
  for j:=0 to 39 do
  begin
    lineao:=img.scanline[j];
    linea:=scanline[j];
    for i:=0 to 39 do
      if lineao[i]<>$F81F then
        linea[i]:=lineao[i]
  end;
end;

//    TTextoDDraw
//**************************************************************
constructor TTextoDDraw.create(Superficie:IDirectDrawSurface7;const FileName:string);
var i,j:integer;
    temp:Tbitmap;
    lindata:Plinea8bits;
    pData:PByte;
begin
  inherited create;
  SuperficieDestino:=Superficie;
  LimitesTexto:=@Rectangulo_Origen;
  alineacionX:=axIzquierda;
  alineacionY:=ayArriba;
  color:=clWhite;
  temp:=CrearDeGDD8bits(filename);
  fancho:=temp.Width;
  falto:=temp.Height;
  fTextHeight:=falto div 7;
  getmem(bitmapTexto,temp.Height*temp.Width);
  pData:=bitmapTexto;
  for j:=0 to temp.Height-1 do
  begin
    lindata:=Temp.ScanLine[j];
    for i:=0 to temp.Width-1 do
    begin
      pData^:=lindata^[i];
      inc(pData);
    end;
  end;
  temp.free;
end;

procedure TTextoDDraw.setColor(Color:Tcolor);
var tempColor:integer;
    lumen:integer;
begin
  if Color=fcolor then exit;
  fcolor:=Color;
  lumen:=(((fcolor and $ff0000)*3) shr 20) + ((fcolor shr 7) and $1ff) +
    (fcolor and $ff);
  colores[3]:=ReducirBitsColor(fcolor);
  tempColor:=((fcolor and $FCFCFC)*3) shr 2;
  colores[2]:=ReducirBitsColor(tempColor);
  if lumen>=255 then
    colores[1]:=ReducirBitsColor((tempColor shr 2) and $3F3F3F)
  else
    colores[1]:=$FFFF;//blanco
end;

function TTextoDDraw.anchoLetra(letra:char):integer;
begin
  case letra of
    #137,
    'l','i','í':result:=3;
    '''',' ','(',')':result:=4;
    '!','¡','j','f','t','I','Í','r','.':result:=5;
    #140,#136,
    'e','s','[',']','a','c','é','á':result:=6;
    #127,#134,#135,#141,#142,#147,#151,#152,#160,#169,#174,#175,
    'm','M','w','W','%','&','@':result:=8;
    else
      result:=7;
  end;
end;

function TTextoDDraw.TextWidth(const texto:string):integer;
var i:integer;
begin
  result:=1;
  for i:=1 to length(texto) do
    inc(result,anchoLetra(texto[i]));
  if result<=1 then result:=0;
end;

function TTextoDDraw.ExtraerTexto(var texto:string; ancho:integer):string;
var i,caracteres,anchoDeUnaLetra,anchoAcumulado:integer;
    cortar:boolean;
begin
  anchoAcumulado:=0;
  caracteres:=0;
  cortar:=false;
  //acumular mientras sea menor o igual al ancho:
  for i:=1 to length(texto) do
  begin
    anchoDeUnaLetra:=anchoLetra(texto[i]);
    if (anchoAcumulado+anchoDeUnaLetra)<=ancho then
    begin
      inc(caracteres);
      inc(anchoAcumulado,anchoDeUnaLetra);
    end
    else
    begin
      cortar:=true;
      break;
    end;
  end;
  //retroceder hasta encontrar un espacio:
  if cortar then
  begin
    inc(caracteres);
    while (texto[caracteres]<>' ') and (texto[caracteres]<>',') do dec(caracteres);
  end;
  result:=copy(texto,1,caracteres);
  if length(result)=0 then
  begin//si es una linea completa que no cabe:
    result:=texto;
    texto:='';
  end
  else
    texto:=copy(texto,caracteres+1,length(texto)-caracteres);
end;

procedure TTextoDDraw.BltIndex(Controlador:pointer;Pitch:integer;const DestR,OrigR:Trect);
var contay,anchoFaltante,anchoFaltanteOrig,anchoY,anchoX,limiteX:integer;
    conta,contaOrig:pointer;
begin
  conta:=pointer(integer(controlador)+destR.top*Pitch+(destR.left shl 1));
  anchoFaltante:=Pitch-(DestR.Right-DestR.Left) shl 1;
  anchoX:=(origR.right-origR.left);
  anchoFaltanteOrig:=fancho-AnchoX;
  anchoY:=(origR.Bottom-origR.top)-1;
  contaOrig:=pointer(integer(bitmapTexto)+origR.top*fancho+origR.left);
  for contay:=0 to anchoY do
  begin
    limiteX:=integer(contaOrig)+anchoX;
    while integer(contaOrig)<limiteX do
    begin
      if byte(contaOrig^)<>0 then
        word(conta^):=colores[byte(contaOrig^)];
      inc(integer(conta),2);
      inc(integer(contaOrig));
    end;
    inc(integer(conta),anchoFaltante);
    inc(integer(contaOrig),anchoFaltanteOrig);
  end;
end;

//NOTA:
//  Trabajar con toda la superficie bloqueada es más rápido que trabajar
//  con parte de la supeficie bloqueada.
//BUG: Cuando Limites texto no tiene la coordenada superior izquierda en (0,0).
//Necesario por lo tanto que esa coordenada sea (left:0,top:0)
function TTextoDDraw.TextOut(X,Y:integer;const texto:string):Trect;
var i,posx,plx,ply,anchocar,anchofijocar,altocar,codigo:integer;
    rorigen,rdestino:Trect;
    puntero:pointer;
    pitch:integer;
begin
  result.bottom:=fTextHeight;
  result.right:=TextWidth(texto);
  if result.right>0 then
  begin
    //Centrar Posición:
    if alineacionX=axDerecha then
      X:=x-result.right
    else if alineacionX=axCentro then
      X:=x-(result.right shr 1){/2};
    if alineacionY=ayAbajo then
      Y:=y-result.bottom
    else if alineacionY=ayCentro then
      Y:=y-(result.bottom shr 1){/2};
    rOrigen:=result;
    rOrigen.top:=0;
    rOrigen.left:=0;
    result.Top:=y;
    result.Left:=x;
    inc(result.bottom,y);
    inc(result.Right,x);
    //SuperficieRender
    if EstaEnInterior(result,rOrigen,LimitesTexto^) then
    if SuperficieDestino.lock(LimitesTexto,DescSuperficieLockUnlock,DDLOCK_WAIT,0)=DD_OK then
    begin
      puntero:=DescSuperficieLockUnlock.lpSurface;
      pitch:=DescSuperficieLockUnlock.lPitch;
      posx:=x;
      altocar:=fTextHeight;
      anchofijocar:=fancho shr 5;{/32}
      for i:=1 to length(texto) do
      begin
        codigo:=ord(texto[i]);
        anchocar:=anchoLetra(texto[i]);
        if codigo>=33 then
        begin
          plx:=(codigo and $1F)*anchofijocar;
          ply:=((codigo shr 5)-1)*altocar;
          rOrigen:=rect(plx,ply,plx+anchocar+1,ply+altocar);
          rDestino:=rect(posx,y,posx+anchocar+1,y+altocar);
          if EstaEnInterior(rDestino,rOrigen,result) then
            BltIndex(puntero,pitch,rDestino,rOrigen);
        end;
        inc(posx,anchocar);
      end;
      SuperficieDestino.unlock(LimitesTexto);
    end;
  end;
end;

destructor TTextoDDraw.destroy;
begin
  freemem(bitmapTexto);
  SuperficieDestino:=nil;
  inherited destroy;
end;

end.
