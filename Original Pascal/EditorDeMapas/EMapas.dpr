program EMapas;

uses
  Forms,
  windows,
  sysutils,
  dialogs,
  main in 'main.pas' {FCmundo},
  SScreen in '..\Laa\SScreen.pas',
  Graficos in '..\Laa\Graficos.pas',
  DirectDraw in '..\Laa\DirectDraw.pas',
  Graficador in '..\Laa\Graficador.pas',
  Demonios in '..\Laa\Demonios.pas',
  objetos in '..\Laa\objetos.pas',
  Tablero in '..\Laa\Tablero.pas',
  def_banderas in 'def_banderas.pas' {F_Banderas},
  DXCommon in '..\Laa\DXCommon.pas',
  cadenas in '..\Laa\cadenas.pas',
  Globales in '..\Laa\Globales.pas';

{$R *.RES}

begin
  Ruta_Aplicacion:='..\Laa\';
  if not fileexists(Ruta_Aplicacion+'grf\logo'+ExtArc) then
  begin
    showmessage('Este programa debe estar ubicado en la carpeta "Editores"');
    exit;
  end;
  Application.Initialize;
  Application.CreateForm(TFCmundo, FCmundo);
  Application.CreateForm(TF_Banderas, F_Banderas);
  Application.Run;
end.
