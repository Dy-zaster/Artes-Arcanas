(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)

unit SScreen;

interface

uses
  Windows,  Graphics,  Forms, Classes, Controls, StdCtrls;

type

//Barra de progreso
  TBarraProgreso=class(TObject)
  //0..100
  private
    canvas:Tcanvas;
    Posicion,
    top,left,
    height,width:smallint;
  public
    constructor create(canvas:Tcanvas;left,top,width,height:smallint);
    procedure StepBy(n:integer);
    procedure Paint;
  end;

  //SplashScreen
  TFEsperar = class(TForm)
  private
    { Private declarations }
    Logo:Tbitmap;
  protected
    procedure paint; override;
  public
    LB_progreso: TLabel;
    PBar:TbarraProgreso;
    destructor destroy; override;
    constructor create(AOwner:Tcomponent); override;
  end;

implementation
{$R TFEsperar.dfm}
uses Graficador,globales,cadenas;

//TBARRA DE PROGRESO
//------------------

constructor TBarraProgreso.create(canvas:Tcanvas;left,top,width,height:smallint);
begin
  self.canvas:=canvas;
  self.left:=left;
  self.top:=top;
  self.width:=width;
  self.height:=height;
  posicion:=0;
end;

procedure TBarraProgreso.StepBy(n:integer);
begin
  n:=posicion+n;
  if n>100 then posicion:=100 else posicion:=n;
  paint;
end;

procedure TBarraProgreso.Paint;
var i:integer;
begin
  with canvas do
    for i:=0 to height-1 do
    begin
      pen.color:=((i*4) and $ff)*$0304;
      moveTo(left,top+i);
      LineTo(left+width*posicion div 100,top+i);
    end;
end;

// TFEsperar
//-----------------------------------------------------------------------------
constructor TFEsperar.create(AOwner:Tcomponent);
begin
  inherited create(AOwner);
  Scaled:=False;
  BorderStyle:=bsNone;
  Ctl3D:=False;
  Color:=clBlack;
  Position:=poScreenCenter;
  Left:=0;
  Top:=0;
  Width:=640;
  Height:=320;
  ParentFont:=True;
  PixelsPerInch:=96;
  controlStyle:=controlStyle+[csOpaque];
  LB_progreso:=TLabel.Create(self);
  with LB_progreso do
  begin
    left:=32;
    top:=280;
    font.PixelsPerInch:=96;// :(
    font.Name:=NOMBRE_FONT;
    font.Size:=16;
    font.Style:=[fsBold,fsItalic];
    font.color:=clWhite;
    caption:='Inicializando Direct X...';
    Transparent:=True
  end;
  LB_progreso.parent:=self;
  PBar:=TBarraProgreso.create(self.canvas,0,304,640,16);
  Logo:=CrearDeGdd(Ruta_Aplicacion+CrptGDD+'logo'+ExtArc);
  brush.Style:=bsClear;
end;

destructor TFEsperar.destroy;
begin
  hide;
  Logo.free;
  Pbar.free;
  inherited destroy;
end;

procedure TFEsperar.Paint;
begin
  self.Canvas.draw(0,0,Logo);
  self.canvas.FillRect(rect(0,280,ClientWidth,320));
  PBar.paint;
end;

end.
