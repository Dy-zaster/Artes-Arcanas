(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)
unit UEstandartes;

interface

uses
  Windows, SysUtils, Graphics, Classes, Forms,Controls, StdCtrls, ExtCtrls,
  GTimer, Gboton;

const
  LimitesFormulario:Trect=(left:0;top:0;right:52;bottom:125);

type
  TRGB=record
    r,g,b:byte;
  end;
  TFEstandartes = class(TForm)
    sb_color: TScrollBar;
    sb_rojo: TScrollBar;
    sb_verde: TScrollBar;
    sb_azul: TScrollBar;
    lbColor: TLabel;
    cb_Predef: TComboBox;
    TimerAnimacion: TTimer;
    procedure FormCreate(Sender: TObject);
    procedure FormDestroy(Sender: TObject);
    procedure sb_rojoChange(Sender: TObject);
    procedure sb_verdeChange(Sender: TObject);
    procedure sb_azulChange(Sender: TObject);
    procedure sb_colorChange(Sender: TObject);
    procedure cb_PredefChange(Sender: TObject);
    procedure GTimer1Timer(Sender: TObject);
    procedure GbotonAceptarClick(Sender: TObject);
    procedure paint; override;
    procedure GbotonCancelarClick(Sender: TObject);
    function execute:boolean;
    procedure FormKeyDown(Sender: TObject; var Key: Word;
      Shift: TShiftState);
  private
    { Private declarations }
    Pantalla:Tbitmap;
    ritmo,fColorActual:integer;
    ColorElegido,DisennoElegido:byte;
    CambioDeEstandarteAceptado:boolean;
    GbotonAceptar,GbotonCancelar:TGboton;
    procedure ActualizarColor;
    procedure ActualizarEstandarte;
    procedure DibujarEstandarte;
    procedure DibujarColorActual;
  public
    { Public declarations }
    quinteto0,quinteto1:integer;
  end;

var
  FEstandartes: TFEstandartes;

implementation
{$R *.DFM}
uses UMensajes,Graficador,graficos,objetos,juego;

type
  TCodigoEstandarte = record
    q0,q1:integer;
  end;

const
  MAX_ESTANDARTES=49;
var
  NivelColorRGB:array[0..9] of TRGB;
  codigos:array[0..MAX_ESTANDARTES] of TCodigoEstandarte;
  familia:array[0..9] of byte;
  cambioSimple:boolean;

procedure DefinirFamilia();
var i,j:integer;
    col:TRGB;
begin
  for i:=0 to 9 do familia[i]:=255;
  for i:=0 to 9 do
  begin
    if (familia[i]<255) then continue;
    familia[i]:=i;
    col:=NivelColorRGB[i];
    for j:=i+1 to 9 do
    begin
      if (familia[j]=255) and
        (NivelColorRGB[j].r=col.r) and
        (NivelColorRGB[j].g=col.g) and
        (NivelColorRGB[j].b=col.b) then
      begin
        familia[j]:=i;
      end;
    end;
  end;
end;

procedure getQuintetos(const cad:string; var q0,q1:integer);
var temp:string;
    posSep:integer;
begin
  temp:=trim(cad);
  posSep:=pos(' ',temp);
  q0:=HexToInt(trim(copy(temp,1,posSep-1)));
  q1:=HexToInt(trim(copy(temp,posSep+1,length(cad))));
end;

procedure TFEstandartes.FormCreate(Sender: TObject);
var ft:textfile;
    cad:string;
    posSep,i:integer;
begin
  GbotonAceptar:=TGboton.create(self);
  with GBotonAceptar do
  begin
    parent:=self;
    Left:=68;
    Top:=230;
    Width:=96;
    Height:=22;
    OnClick:=GbotonAceptarClick;
    Color:=clBronce;
    Caption:='Aceptar';
  end;
  GbotonCancelar:=TGboton.create(self);
  with GBotonCancelar do
  begin
    parent:=self;
    Left:=200;
    Top:=230;
    Width:=96;
    Height:=22;
    OnClick:=GbotonCancelarClick;
    Color:=clBronce;
    Caption:='Cancelar';
  end;
  ControlStyle:=ControlStyle+[csOpaque];
  Pantalla:=Tbitmap.create;
  with pantalla do
  begin
    handleType:=bmdib;
    PixelFormat:=pf16bit;
    Width:=LimitesFormulario.right;
    height:=LimitesFormulario.bottom;
    BitBlt(canvas.handle,0,0,Width,Height,0,0,0,BLACKNESS);
  end;
  //Para preparar las tablas de colores indexados:
  ritmo:=0;
  ColorElegido:=0;
  {$I-}
  assignFile(ft,'estandartes.txt');
  reset(ft);
  i:=0;
  while not eof(ft) do
  begin
    readln(ft,cad);
    posSep:=pos(':',cad);
    if (cad<>'') and (posSep>0) then
    begin
      cb_Predef.Items.Add(Copy(cad,1,posSep-1));
      getQuintetos(Copy(cad,posSep+1,length(cad)),
        codigos[i].q0,codigos[i].q1);
      inc(i);
      if (i>MAX_ESTANDARTES) then break;
    end;
  end;
  closefile(ft);
  {$I+}
  //solo para leer IOresult
  if IOresult<>0 then ritmo:=0;
  cambioSimple:=false;
  quinteto0:=0;
  quinteto1:=0;
  canvas.brush.style:=bsSolid;
end;

procedure TFEstandartes.paint;
begin
  PintarFondoNegro(self);
  with canvas do
  begin
    TextOut(20,24,'DISEÑOS PREDEFINIDOS:');
    TextOut(20,92,'Color:');
    TextOut(20,124,'Rojo:');
    TextOut(20,156,'Verde:');
    TextOut(20,188,'Azul:');
  end;
  DibujarEstandarte;
  DibujarColorActual;
end;

procedure TFEstandartes.DibujarColorActual;
begin
  with canvas do
  begin
    brush.Style:=bsSolid;
    brush.color:=fColorActual;
    FrameRect(rect(218,120,334,202));
    FillRect(rect(221,123,331,199));
  end;
end;

procedure TFEstandartes.FormDestroy(Sender: TObject);
begin
  TimerAnimacion.free;
  Pantalla.free;
end;

procedure TFEstandartes.sb_rojoChange(Sender: TObject);
var n,f,i:integer;
begin
  n:=TScrollBar(sender).position;
  if cambioSimple then
     NivelColorRGB[ColorElegido].r:=n
  else
  begin
    f:=familia[ColorElegido];
    for i:=0 to 9 do
      if (familia[i]=f) then NivelColorRGB[i].r:=n;
  end;
  ActualizarColor;
end;

procedure TFEstandartes.sb_verdeChange(Sender: TObject);
var n,f,i:integer;
begin
  n:=TScrollBar(sender).position;
  if cambioSimple then
     NivelColorRGB[ColorElegido].g:=n
  else
  begin
    f:=familia[ColorElegido];
    for i:=0 to 9 do
      if (familia[i]=f) then NivelColorRGB[i].g:=n;
  end;
  ActualizarColor;
end;

procedure TFEstandartes.sb_azulChange(Sender: TObject);
var n,f,i:integer;
begin
  n:=TScrollBar(sender).position;
  if cambioSimple then
     NivelColorRGB[ColorElegido].b:=n
  else
  begin
    f:=familia[ColorElegido];
    for i:=0 to 9 do
      if (familia[i]=f) then NivelColorRGB[i].b:=n;
  end;
  ActualizarColor;
end;

procedure TFEstandartes.ActualizarColor;
    procedure PrepararQuintuple(var Quintuple:integer;base:integer);
    var i:integer;
    begin
      Quintuple:=0;
      for i:=base to base+4 do
      begin
        Quintuple:=Quintuple shl 6;
        Quintuple:=Quintuple or
          ((NivelColorRGB[i].r) shl 4) or
          ((NivelColorRGB[i].g) shl 2) or
          (NivelColorRGB[i].b);
      end;
    end;
  procedure PrepararQuintuples;
  begin
    PrepararQuintuple(Quinteto0,0);
    Quinteto0:=Quinteto0 or $40000000;
    PrepararQuintuple(Quinteto1,5);
    Quinteto1:=Quinteto1 or (DisennoElegido shl 30);
  end;
begin
  PrepararQuintuples;
  PrepararTablaColores(Quinteto0,Quinteto1);
  fColorActual:=ColorDeLaTabla((4-(ColorElegido mod 5))+((ColorElegido div 5)*5));
  DibujarColorActual;
end;

procedure TFEstandartes.sb_colorChange(Sender: TObject);
begin
  ColorElegido:=sb_color.position;
  cambioSimple:=true;
  sb_rojo.Position:=NivelColorRGB[ColorElegido].r;
  sb_verde.Position:=NivelColorRGB[ColorElegido].g;
  sb_azul.Position:=NivelColorRGB[ColorElegido].b;
  cambioSimple:=false;
  ActualizarColor;
  LbColor.Caption:=inttostr(ColorElegido);
end;

procedure TFEstandartes.ActualizarEstandarte;
  procedure ExtraerQuinteto(quinteto,Base:integer);
  var i:integer;
  begin
    for i:=Base+4 downto Base do
    begin
      NivelColorRGB[i].r:=(quinteto and $30) shr 4;
      NivelColorRGB[i].g:=(quinteto and $0C) shr 2;
      NivelColorRGB[i].b:=(quinteto and $03);
      quinteto:=quinteto shr 6;//recorrer lo extraido
    end;
  end;
begin
  //Actualizar NivelColorRGB
  ExtraerQuinteto(Quinteto0,0);
  ExtraerQuinteto(Quinteto1,5);
  disennoElegido:=Quinteto1 shr 30;
  ActualizarColor;
  cambioSimple:=true;
  sb_rojo.Position:=NivelColorRGB[ColorElegido].r;
  sb_verde.Position:=NivelColorRGB[ColorElegido].g;
  sb_azul.Position:=NivelColorRGB[ColorElegido].b;
  cambioSimple:=false;
end;

procedure TFEstandartes.cb_PredefChange(Sender: TObject);
begin
  with codigos[cb_predef.ItemIndex] do
  begin
    quinteto0:=q0;
    quinteto1:=q1;
  end;
  ActualizarEstandarte();
  DefinirFamilia();
end;

procedure TFEstandartes.GTimer1Timer(Sender: TObject);
var ancho,alto,frame,posY:integer;
    ElEstandarte:TAnimacionEfecto;
begin
  inc(ritmo);
  frame:=ritmo and $7;
  with pantalla do
    BitBlt(canvas.handle,0,0,Width,Height,0,0,0,BLACKNESS);
  ElEstandarte:=TAnimacionEfecto(animas.animacion[192{AnEstandarte en Demonios.pas}+disennoElegido]);
  if not (ElEstandarte is TAnimacionEfecto) then exit;
  ancho:=ElEstandarte.miSuperficie.width;
  if frame=0 then
  begin
    posy:=0;
    alto:=ElEstandarte.miPosicionA.acumy[frame];
  end
  else
  begin
    posY:=ElEstandarte.miPosicionA.acumy[frame-1];
    alto:=ElEstandarte.miPosicionA.acumy[frame]-posY;
  end;
  PrepararTablaColores(quinteto0,quinteto1);
  blt0TransTablaColores(pantalla,ElEstandarte.miPosicionA.cenx[frame],ElEstandarte.miPosicionA.ceny[frame],ancho,alto,ElEstandarte.miSuperficie,0,posy);
  DibujarEstandarte;
end;

procedure TFEstandartes.DibujarEstandarte;
begin
  Canvas.StretchDraw(rect(356,12,LimitesFormulario.right*2+356,LimitesFormulario.bottom*2+12),pantalla);
end;

procedure TFEstandartes.GbotonAceptarClick(Sender: TObject);
begin
  CambioDeEstandarteAceptado:=True;
  close;
end;

function TFEstandartes.execute:boolean;
begin
  ActualizarEstandarte;
  DefinirFamilia();
  CambioDeEstandarteAceptado:=false;
  TimerAnimacion.enabled:=true;
  showmodal;
  TimerAnimacion.enabled:=false;
  result:=CambioDeEstandarteAceptado;
end;

procedure TFEstandartes.GbotonCancelarClick(Sender: TObject);
begin
  close;
end;

procedure TFEstandartes.FormKeyDown(Sender: TObject; var Key: Word;
  Shift: TShiftState);
begin
  if (key=27) or (key=13) then
  begin
    if (key=13) then CambioDeEstandarteAceptado:=True;
    close;
  end;
end;

end.

