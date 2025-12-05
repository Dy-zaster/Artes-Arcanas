unit Estandartes;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls, ExtCtrls;

const
  MaxCuadrosM=7;
  EXT_CRG='.cr9';
type
  TRGB=record
    r,g,b:byte;
  end;
  TdaMonstruo=record
      anchoMax:smallint;
      modix,modiy:byte;
      acumy:array[0..MaxCuadrosM] of smallint;
      ancho:array[0..MaxCuadrosM] of byte;
      cenx,ceny:array[0..MaxCuadrosM] of byte
    end;

  TForm1 = class(TForm)
    PantallaRender: TPaintBox;
    Label1: TLabel;
    cb_disenno: TComboBox;
    sb_color: TScrollBar;
    Label2: TLabel;
    sb_rojo: TScrollBar;
    Label3: TLabel;
    Panel1: TPanel;
    sb_verde: TScrollBar;
    Label4: TLabel;
    sb_azul: TScrollBar;
    Label5: TLabel;
    Bevel1: TBevel;
    Label6: TLabel;
    Edit1: TEdit;
    Timer1: TTimer;
    lbColor: TLabel;
    Button1: TButton;
    cb_Predef: TComboBox;
    Label7: TLabel;
    CB_Mantener: TCheckBox;
    procedure FormCreate(Sender: TObject);
    procedure FormDestroy(Sender: TObject);
    procedure PantallaRenderPaint(Sender: TObject);
    procedure FormActivate(Sender: TObject);
    procedure Timer1Timer(Sender: TObject);
    procedure cb_disennoChange(Sender: TObject);
    procedure sb_rojoChange(Sender: TObject);
    procedure sb_verdeChange(Sender: TObject);
    procedure sb_azulChange(Sender: TObject);
    procedure sb_colorChange(Sender: TObject);
    procedure UsarCodigo(Sender: TObject);
    procedure cb_PredefChange(Sender: TObject);
    procedure CB_MantenerClick(Sender: TObject);
  private
    { Private declarations }
    Pantalla:Tbitmap;
    Bandera:array[0..3] of Tbitmap;
    posicionA:array[0..3] of TdaMonstruo;
    ColorElegido,DisennoElegido:byte;
    Quintuple0,Quintuple1,ritmo:integer;
    procedure ActualizarColor;
    procedure drawFlag;
  public
    { Public declarations }
  end;

var
  Form1: TForm1;

implementation

{$R *.DFM}
var

    TablaIndexadaDeColores:array[0..255] of word;//Visible para BltTablaColor
    TablaEscala:array[0..63] of byte;//Para crear la tabla indexada de colores;
    QuintupleColores0,QuintupleColores1:integer;//Colores de los estandartes. (30 bits en cada integer)

    NivelColorRGB:array[0..9] of TRGB;
    familia:array[0..9] of byte;
    cambiarFamilia:boolean;
    codigos:TStrings;

procedure DefinirFamilia();
var i,j:integer;
    col:TRGB;
begin
  if (not cambiarFamilia) then exit;
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

procedure TForm1.FormCreate(Sender: TObject);
var i,posSep:integer;
    f:file of TdaMonstruo;
    ft:textfile;
    cad:string;
begin
  randomize;
  cambiarFamilia:=true;
  DisennoElegido := 0;
  cb_disenno.ItemIndex:=0;
  ControlStyle:=ControlStyle+[csOpaque];
  with PantallaRender do
    ControlStyle:=ControlStyle+[csOpaque];
  Pantalla:=Tbitmap.create;
  with pantalla do
  begin
    handleType:=bmdib;
    PixelFormat:=pf16bit;
    Width:=pantallaRender.width;
    height:=pantallarender.height;
    BitBlt(canvas.handle,0,0,Width,Height,0,0,0,BLACKNESS);
  end;
  //Para preparar las tablas de colores indexados:
  for i:=0 to 15 do
    TablaEscala[i]:=0;
  for i:=0 to 15 do
    TablaEscala[16+i]:=i*8+8;
  for i:=0 to 15 do
    TablaEscala[32+i]:=i*8+64;
  for i:=0 to 15 do
    TablaEscala[48+i]:=i*8+135;

  for i:=0 to 3 do
  begin
    Bandera[i]:=Tbitmap.create;

    assignfile(f,'..\LAA\grf\b'+inttostr(i)+EXT_CRG);
    FileMode:=0;
    reset(f);
    read(f,posicionA[i]);
    closeFile(f);

    with Bandera[i] do
    begin
      LoadFromFile('..\LAA\grf\b'+inttostr(i)+'.bmp');
      pixelFormat:=pf8bit;
      //dormant;
    end;
  end;

  codigos:=TStringList.Create();
  ritmo:=0;
  ColorElegido:=0;
  assignFile(ft,'..\Laa\estandartes.txt');
  reset(ft);
  while not eof(ft) do
  begin
    readln(ft,cad);
    posSep:=pos(':',cad);
    if (cad<>'') and (posSep>0) then
    begin
      cb_Predef.Items.Add(Copy(cad,1,posSep-1));
      codigos.Add(Copy(cad,posSep+1,length(cad)));
    end;
  end;
  closefile(ft);
end;

procedure TForm1.FormDestroy(Sender: TObject);
var i:integer;
begin
  codigos.Free;
  Pantalla.free;
  for i:=3 downto 0 do
    Bandera[i].free;
end;

function ReducirBitsColor(Color32:Tcolor):word;
begin
  result:=(Color32 and $0000F8) shl 8 or
            (Color32 and $00FC00) shr 5 or
            (Color32 and $F80000) shr 19;
end;

function EstaEnInterior(var XDes,YDes,Ancho,Alto,XOrg,YOrg:integer;const rLimites:Trect):bytebool;
//  Determina si la imagen que debe ser dibujada aparece en pantalla y modifica
//los rectángulos origen y destino si estos exceden las dimensiones adecuadas.
//Mientras que el rectángulo destino puede salir de los límites de la superficie destino
//y será corregido todo para evitarlo, no corrige si esto sucede en el caso del origen
//El "rectángulo" origen
begin
 if (XDes<rLimites.right) and ((XDes+Ancho)>=rLimites.left) and
   (YDes<rLimites.bottom) and ((YDes+Alto)>=rLimites.top) then
 begin
   result:=true;
   //Reducir el tamaño del cuadro, posiciones origen no modificadas.
   if Ancho>rLimites.right-XDes then
     Ancho:=rLimites.right-XDes;
   if Alto>rLimites.bottom-YDes then
     Alto:=rLimites.bottom-YDes;
   //Reducir tamaño, también modificar posiciones origen
   if XDes<rLimites.left then
   begin
     dec(Ancho,rLimites.left-XDes);
     inc(XOrg,rLimites.left-XDes);
     XDes:=rLimites.left;
   end;
   if YDes<rLimites.top then
   begin
     dec(Alto,rLimites.top-YDes);
     inc(YOrg,rLimites.top-YDes);
     YDes:=rLimites.top;
   end;
 end
 else
   result:=false;
end;

const
  mskTrans=$7BDE;
  LimitesPantalla:Trect=(left:0;top:0;right:640;bottom:480);
type
  Tlinea16=array[0..640] of word;
  Tlinea8=array[0..640] of byte;

function blt0Trans(BitmapDes:Tbitmap;XDes,YDes,Ancho,Alto:integer;BitmapOrg:Tbitmap;XOrg,YOrg:integer):boolean;
var linea:^Tlinea16;
    lineao:^Tlinea8;
    i,j,Ajuste:integer;
begin
  result:=EstaEnInterior(XDes,YDes,Ancho,Alto,XOrg,YOrg,LimitesPantalla);
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
          (TablaEscala[r+i])+
          (TablaEscala[g+i] shl 8)+
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

procedure TForm1.PantallaRenderPaint(Sender: TObject);
begin
  with pantallaRender do
    Canvas.StretchDraw(rect(0,0,width *3,height*3),pantalla);
end;

procedure TForm1.FormActivate(Sender: TObject);
begin
  drawFlag();
  Timer1.Enabled:=true;
end;

procedure TForm1.drawFlag();
var ancho,alto,frame,posY:integer;
begin
  inc(ritmo);
  frame:=ritmo and $7;
  with pantalla do
    BitBlt(canvas.handle,0,0,Width,Height,0,0,0,BLACKNESS);
  ancho:=bandera[disennoElegido].width;
  if frame=0 then
  begin
    posy:=0;
    alto:=posicionA[disennoElegido].acumy[frame];
  end
  else
  begin
    posY:=posicionA[disennoElegido].acumy[frame-1];
    alto:=posicionA[disennoElegido].acumy[frame]-posY;
  end;
  blt0Trans(pantalla,posicionA[disennoElegido].cenx[frame],posicionA[disennoElegido].ceny[frame],ancho,alto,bandera[disennoElegido],0,posy);
  PantallaRender.Repaint;
end;

procedure TForm1.Timer1Timer(Sender: TObject);
begin
  drawFlag();
end;

procedure TForm1.cb_disennoChange(Sender: TObject);
begin
  disennoElegido:=cb_disenno.ItemIndex and $3;
  ActualizarColor;
end;

procedure TForm1.sb_rojoChange(Sender: TObject);
var n,f,i:integer;
begin
  n:=TScrollBar(sender).position;
  if (CB_Mantener.Checked) then
  begin
    f:=familia[ColorElegido];
    for i:=0 to 9 do
      if (familia[i]=f) then NivelColorRGB[i].r:=n;
  end
  else
    NivelColorRGB[ColorElegido].r:=n;
  ActualizarColor;
end;

procedure TForm1.sb_verdeChange(Sender: TObject);
var n,f,i:integer;
begin
  n:=TScrollBar(sender).position;
  if (CB_Mantener.Checked) then
  begin
    f:=familia[ColorElegido];
    for i:=0 to 9 do
      if (familia[i]=f) then NivelColorRGB[i].g:=n;
  end
  else
    NivelColorRGB[ColorElegido].g:=n;
  ActualizarColor;
end;

procedure TForm1.sb_azulChange(Sender: TObject);
var n,f,i:integer;
begin
  n:=TScrollBar(sender).position;
  if (CB_Mantener.Checked) then
  begin
    f:=familia[ColorElegido];
    for i:=0 to 9 do
      if (familia[i]=f) then NivelColorRGB[i].b:=n;
  end
  else
    NivelColorRGB[ColorElegido].b:=n;
  ActualizarColor;
end;

procedure Tform1.ActualizarColor;
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
    PrepararQuintuple(Quintuple0,0);
    Quintuple0:=Quintuple0 or $40000000;
    PrepararQuintuple(Quintuple1,5);
    Quintuple1:=Quintuple1 or (DisennoElegido shl 30);
  end;
begin
  Panel1.color:=TablaEscala[NivelColorRGB[ColorElegido].r shl 4+15] or
    (TablaEscala[NivelColorRGB[ColorElegido].g shl 4+15] shl 8) or
    (TablaEscala[NivelColorRGB[ColorElegido].b shl 4+15] shl 16);
  PrepararQuintuples;
  PrepararTablaColores(Quintuple0,Quintuple1);
  Edit1.text:=IntToHex(Quintuple0,8)+' '+IntToHex(Quintuple1,8);
end;

procedure TForm1.sb_colorChange(Sender: TObject);
begin
  ColorElegido:=sb_color.position;
  cambiarFamilia:=false;
  sb_rojo.Position:=NivelColorRGB[ColorElegido].r;
  sb_verde.Position:=NivelColorRGB[ColorElegido].g;
  sb_azul.Position:=NivelColorRGB[ColorElegido].b;
  cambiarFamilia:=true;
  ActualizarColor;
  LbColor.Caption:=inttostr(ColorElegido);
end;

function HexToInt(const cadenaHex:string):integer;
var i:integer;
    c:char;
begin
  result:=0;
  for i:=1 to length(cadenaHex) do
  begin
    result:=result shl 4;
    c:=upcase(cadenaHex[i]);
    case c of
      '0'..'9':inc(result,ord(c)-ord('0'));
      'A'..'F':inc(result,ord(c)-ord('A')+10);
    end;
  end;
end;

procedure TForm1.UsarCodigo(Sender: TObject);
var
  cad0,cad1:string;
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
  cad0:=trim(edit1.text);
  cad1:=trim(copy(cad0,pos(' ',cad0)+1,length(cad0)));
  cad0:=trim(copy(cad0,1,pos(' ',cad0)-1));
  Quintuple0:=HexToInt(cad0);
  Quintuple1:=HexToInt(cad1);
//  showmessage(inttohex(HexToInt(inttohex(quintuple0,8)),8));
  ExtraerQuinteto(Quintuple0,0);
  ExtraerQuinteto(Quintuple1,5);
  disennoElegido:=Quintuple1 shr 30;
  cb_disenno.itemIndex:=disennoElegido;
  DefinirFamilia();
  ActualizarColor;
  cambiarFamilia:=false;
  sb_rojo.Position:=NivelColorRGB[ColorElegido].r;
  sb_verde.Position:=NivelColorRGB[ColorElegido].g;
  sb_azul.Position:=NivelColorRGB[ColorElegido].b;
  cambiarFamilia:=true;
end;

procedure TForm1.cb_PredefChange(Sender: TObject);
begin
  Edit1.text:=codigos[cb_predef.itemIndex];
  UsarCodigo(nil);
end;

procedure TForm1.CB_MantenerClick(Sender: TObject);
begin
  if (TCheckBox(sender).checked) then DefinirFamilia();
end;

end.
