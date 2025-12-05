program EditorEstandartes;

uses
  Forms,
  sysutils,
  dialogs,
  Estandartes in 'Estandartes.pas' {Form1};

{$R *.RES}

begin
  if not fileexists('..\LAA\grf\b0.bmp') then
  begin
    showmessage('Este programa debe estar ubicado en la carpeta "Editores"');
    exit;
  end;
  Application.Initialize;
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.


