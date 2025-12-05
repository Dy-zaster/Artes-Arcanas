{*******************************************************}
{                                                       }
{       Delphi Visual Component Library                 }
{                                                       }
{       Copyright (c) 1995,97 Borland International     }
{                                                       }
{*******************************************************}
{
Editado por Sergio:
Básicamente es una version menor del MPlayer independiente de TWinControl
}

unit MPlayerLite;

{$R-}

interface

uses Windows,  MMSystem, SysUtils;

type
  TMPLastAction=(mpaNone,mpaOpen,mpaClose,mpaPlay,mpaStop,mpaPause,mpaResume,mpaSeek,mpaVolume);

  TMPDeviceTypes = (dtAutoSelect, dtAVIVideo, dtSequencer, dtMPEGVideo, dtMp3);

  TMPTimeFormats = (tfMilliseconds, tfHMS, tfFrames, tfSMPTE24, tfSMPTE25,
    tfSMPTE30, tfSMPTE30Drop, tfBytes, tfSamples);
  TMPModes = (mpNotReady, mpStopped, mpPlaying, mpRecording, mpSeeking,
    mpPaused, mpOpen);

  TMPDevCaps = (mpCanStep, mpCanPlay, mpUsesWindow);
  TMPDevCapsSet = set of TMPDevCaps;

  EMCIDeviceError = class(Exception);

  TMediaPlayer = class(TObject)
  private
    fLastAction: TMPLastAction;
    MCIOpened: Boolean;
    FCapabilities: TMPDevCapsSet;
    FCanPlay: Boolean;
    FCanStep: Boolean;
    FFlags: Longint;
    FWait: Boolean;
    FUseWait: Boolean;
    FUseFrom: Boolean;
    FUseTo: Boolean;
    FDeviceID: Word;
    FDeviceType: TMPDeviceTypes;
    FTo: Longint;
    FFrom: Longint;
    FFrames: Longint;
    FError: Longint;
    FDWidth: Integer;
    FDHeight: Integer;
    FElementName: string;
    FAutoOpen: Boolean;
    FAutoRewind: Boolean;
    FShareable: Boolean;
    Handle: Thandle;

    function CheckIfOpen():boolean;
    procedure SetPosition(Value: Longint);
    procedure SetDeviceType( Value: TMPDeviceTypes );
    procedure SetWait( Flag: Boolean );
    procedure SetFrom( Value: Longint );
    procedure SetTo( Value: Longint );
    procedure GetDeviceCaps;
    function GetStart: Longint;
    function GetLength: Longint;
    function GetMode: TMPModes;
    function GetTracks: Longint;
    function GetPosition: Longint;
    function GetErrorMessage: string;
    function GetTimeFormat: TMPTimeFormats;
    function GetTrackLength(TrackNum: Integer): Longint;
    function GetTrackPosition(TrackNum: Integer): Longint;
  public
    destructor Destroy; override;  
    constructor Create(aHandle: THandle);
    procedure Open;
    procedure Close;
    procedure Play;
    procedure Stop;
    procedure Pause; {Pause & Resume/Play}
    procedure Previous;
    procedure Next;
    procedure PauseOnly;
    procedure Resume;
    procedure Rewind;
    property TrackLength[TrackNum: Integer]: Longint read GetTrackLength;
    property TrackPosition[TrackNum: Integer]: Longint read GetTrackPosition;
    property Capabilities: TMPDevCapsSet read FCapabilities;
    property Error: Longint read FError;
    property ErrorMessage: string read GetErrorMessage;
    property Start: Longint read GetStart;
    property Length: Longint read GetLength;
    property Tracks: Longint read GetTracks;
    property Frames: Longint read FFrames write FFrames;
    property Mode: TMPModes read GetMode;
    property LastAction: TMPLastAction read fLastAction;
    property Position: Longint read GetPosition write SetPosition;
    property Wait: Boolean read FWait write SetWait;
    property StartPos: Longint read FFrom write SetFrom;
    property EndPos: Longint read FTo write SetTo;
    property DeviceID: Word read FDeviceID;
    property TimeFormat: TMPTimeFormats read GetTimeFormat;
  published
    property AutoOpen: Boolean read FAutoOpen write FAutoOpen default False;
    property AutoRewind: Boolean read FAutoRewind write FAutoRewind default True;
    property DeviceType: TMPDeviceTypes read FDeviceType write SetDeviceType default dtAutoSelect;
    property FileName: string read FElementName write FElementName;
    property Shareable: Boolean read FShareable write FShareable default False;
  end;

implementation

constructor TMediaPlayer.Create(aHandle: THandle);
begin
  Handle:= aHandle;
  FAutoOpen := False;
  FAutoRewind := True;
  FDeviceType := dtAutoSelect; {select through file name extension}
  fLastAction:= mpaNone;
end;

destructor TMediaPlayer.Destroy;
var
  GenParm: TMCI_Generic_Parms;
begin
  if FDeviceID <> 0 then
    mciSendCommand( FDeviceID, mci_Close, mci_Wait, Longint(@GenParm));
end;

{for MCI Commands to make sure device is open}
function TMediaPlayer.CheckIfOpen():boolean;
begin
  result:= MCIOpened;
end;

{***** MCI Commands *****}

procedure TMediaPlayer.Open;
const
  DeviceName: array[TMPDeviceTypes] of PChar = ('', 'AVIVideo', 'Sequencer',
    'MPEGVideo', 'MPEGVideo');
var
  OpenParm: TMCI_Open_Parms;
begin
  if MCIOpened then Close; {must close MCI Device first before opening another}
  FillChar(OpenParm,SizeOf(OpenParm),0);
  OpenParm.dwCallback := Handle;
  if FDeviceType <> dtAutoSelect then {fill in Device Type}
   OpenParm.lpstrDeviceType := DeviceName[FDeviceType];
  if FElementName <> '' then
    OpenParm.lpstrElementName := PChar(FElementName);
  FFlags := 0;
  if FUseWait then
  begin
    if FWait then FFlags := mci_Wait;
    FUseWait := False;
  end
  else FFlags := mci_Wait;
  if FElementName <> '' then FFlags := FFlags or mci_Open_Element;
  if FDeviceType <> dtAutoSelect then FFlags := FFlags or mci_Open_Type;
  if FShareable then FFlags := FFlags or mci_Open_Shareable;
  OpenParm.dwCallback := Handle;
  fLastAction:= mpaOpen;
  FError := mciSendCommand(0,MCI_OPEN, FFlags, Longint(@OpenParm));
  if FError <> 0 then {problem opening device}
    raise EMCIDeviceError.Create(ErrorMessage)
  else {device successfully opened}
  begin
    MCIOpened := True;
    FDeviceID := OpenParm.wDeviceID;
    FFrames := Length div 10;  {default frames to step = 10% of total frames}
    GetDeviceCaps; {must first get device capabilities}
  end;
end;

procedure TMediaPlayer.Close;
var
  GenParm: TMCI_Generic_Parms;
begin
  if FDeviceID <> 0 then
  begin
    FFlags := 0;
    if FUseWait then
    begin
      if FWait then FFlags := mci_Wait;
      FUseWait := False;
    end
    else FFlags := mci_Wait;
    GenParm.dwCallback := Handle;
    fLastAction:= mpaClose;
    FError := mciSendCommand( FDeviceID, mci_Close, FFlags, Longint(@GenParm));
    if FError = 0 then
    begin
      MCIOpened := False;
      FDeviceID := 0;
    end;
  end; {if DeviceID <> 0}
end;

procedure TMediaPlayer.Play;
var
  PlayParm: TMCI_Play_Parms;
begin
  if not CheckIfOpen() then exit;

  {if at the end of media, and not using StartPos or EndPos - go to start}
  if FAutoRewind and (Position = Length) then
    if not FUseFrom and not FUseTo then Rewind;

  FFlags := mci_Notify;
  if FUseWait then
  begin
    if FWait then FFlags := FFlags or mci_Wait;
    FUseWait := False;
  end;
  if FUseFrom then
  begin
    FFlags := FFlags or mci_From;
    PlayParm.dwFrom := FFrom;
    FUseFrom := False; {only applies to this mciSendCommand}
  end;
  if FUseTo then
  begin
    FFlags := FFlags or mci_To;
    PlayParm.dwTo := FTo;
    FUseTo := False; {only applies to this mciSendCommand}
  end;
  PlayParm.dwCallback := Handle;
  fLastAction:= mpaPlay;
  FError := mciSendCommand( FDeviceID, mci_Play, FFlags, Longint(@PlayParm));
end;

procedure TMediaPlayer.Stop;
var
  GenParm: TMCI_Generic_Parms;
begin
  if not CheckIfOpen() then exit;

  FFlags := 0;
  if FUseWait then
  begin
    if FWait then FFlags := mci_Wait;
    FUseWait := False;
  end
  else FFlags := mci_Wait;
  GenParm.dwCallback := Handle;
  fLastAction:= mpaStop;
  FError := mciSendCommand( FDeviceID, mci_Stop, FFlags, Longint(@GenParm));
end;

procedure TMediaPlayer.Pause;
begin
  if not CheckIfOpen() then exit;
  if Mode = mpPlaying then PauseOnly
  else
   if Mode = mpPaused then Resume;
end;

procedure TMediaPlayer.PauseOnly;
var
  GenParm: TMCI_Generic_Parms;
begin
  if not CheckIfOpen() then exit;

  FFlags := 0;
  if FUseWait then
  begin
    if FWait then FFlags := mci_Wait;
    FUseWait := False;
  end
  else FFlags := mci_Wait;
  GenParm.dwCallback := Handle;
  fLastAction:= mpaPause;
  FError := mciSendCommand( FDeviceID, mci_Pause, FFlags, Longint(@GenParm));
end;

procedure TMediaPlayer.Resume;
var
  GenParm: TMCI_Generic_Parms;
begin
  if not CheckIfOpen() then exit;

  FFlags := mci_Notify;
  if FUseWait then
  begin
    if FWait then FFlags := FFlags or mci_Wait;
  end;
  GenParm.dwCallback := Handle;
  fLastAction:= mpaResume;
  FError := mciSendCommand( FDeviceID, mci_Resume, FFlags, Longint(@GenParm));

  {if error calling resume (resume not supported),  call Play}
  if FError <> 0 then
    Play {FUseWait reset by Play}
  else
  begin
    if FUseWait then
      FUseWait := False;
  end;
end;

procedure TMediaPlayer.Next;
var
  SeekParm: TMCI_Seek_Parms;
  TempFlags: Longint;
begin
  if not CheckIfOpen() then exit;

  FFlags := 0;
  if FUseWait then
  begin
    if FWait then FFlags := mci_Wait;
    FUseWait := False;
  end
  else FFlags := mci_Wait;

  TempFlags := FFlags; {preserve FFlags from GetTimeFormat & GetPosition}
  FFlags := TempFlags or mci_Seek_To_End;
    
  SeekParm.dwCallback := Handle;
  fLastAction:= mpaSeek;
  FError := mciSendCommand( FDeviceID, mci_Seek, FFlags, Longint(@SeekParm));
end; {Next}


procedure TMediaPlayer.Previous;
var
  SeekParm: TMCI_Seek_Parms;
  TempFlags: Longint;
begin
  if not CheckIfOpen() then exit;

  FFlags := 0;
  if FUseWait then
  begin
    if FWait then FFlags := mci_Wait;
    FUseWait := False;
  end
  else FFlags := mci_Wait;
  
  TempFlags := FFlags; {preserve FFlags from GetTimeFormat & GetPosition}
  FFlags := TempFlags or mci_Seek_To_Start;
    
  SeekParm.dwCallback := Handle;
  fLastAction:= mpaSeek;  
  FError := mciSendCommand( FDeviceID, mci_Seek, FFlags, Longint(@SeekParm));
end; {Previous}

procedure TMediaPlayer.SetPosition(Value: Longint);
var
  SeekParm: TMCI_Seek_Parms;
begin
  if not CheckIfOpen() then exit;

  FFlags := 0;
  if FUseWait then
  begin
    if FWait then FFlags := mci_Wait;
    FUseWait := False;
  end
  else FFlags := mci_Wait;

  FFlags := FFlags or mci_To;
  SeekParm.dwCallback := Handle;
  SeekParm.dwTo := Value;
  fLastAction:= mpaSeek;
  FError := mciSendCommand( FDeviceID, mci_Seek, FFlags, Longint(@SeekParm));
end;

procedure TMediaPlayer.Rewind;
var
  SeekParm: TMCI_Seek_Parms;
  RFlags: Longint;
begin
  if not CheckIfOpen() then exit;
  RFlags := mci_Wait or mci_Seek_To_Start;
  fLastAction:= mpaSeek;
  mciSendCommand( FDeviceID, mci_Seek, RFlags, Longint(@SeekParm));
end;

function TMediaPlayer.GetTrackLength(TrackNum: Integer): Longint;
var
  StatusParm: TMCI_Status_Parms;
begin
  if not CheckIfOpen() then
  begin
    result:= 0;
    exit;
  end;
  FFlags := mci_Wait or mci_Status_Item or mci_Track;
  StatusParm.dwItem := mci_Status_Length;
  StatusParm.dwTrack := Longint(TrackNum);
  mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := StatusParm.dwReturn;
end;

function TMediaPlayer.GetTrackPosition(TrackNum: Integer): Longint;
var
  StatusParm: TMCI_Status_Parms;
begin
  FFlags := mci_Wait or mci_Status_Item or mci_Track;
  StatusParm.dwItem := mci_Status_Position;
  StatusParm.dwTrack := Longint(TrackNum);
  mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := StatusParm.dwReturn;
end;

{*** procedures that set control flags for MCI Commands ***}
procedure TMediaPlayer.SetWait( Flag: Boolean );
begin
  if Flag <> FWait then FWait := Flag;
  FUseWait := True;
end;

procedure TMediaPlayer.SetFrom( Value: Longint );
begin
  if Value <> FFrom then FFrom := Value;
  FUseFrom := True;
end;

procedure TMediaPlayer.SetTo( Value: Longint );
begin
  if Value <> FTo then FTo := Value;
  FUseTo := True;
end;


procedure TMediaPlayer.SetDeviceType( Value: TMPDeviceTypes );
begin
  if Value <> FDeviceType then FDeviceType := Value;
end;

{ fills in static properties upon opening MCI Device }
procedure TMediaPlayer.GetDeviceCaps;
var
  DevCapParm: TMCI_GetDevCaps_Parms;
  devType: Longint;
  RectParms: TMCI_Anim_Rect_Parms;
  WorkR: TRect;
begin
  FFlags := mci_Wait or mci_GetDevCaps_Item;

  DevCapParm.dwItem := mci_GetDevCaps_Can_Play;
  mciSendCommand(FDeviceID, mci_GetDevCaps, FFlags,  Longint(@DevCapParm) );
  FCanPlay := Boolean(DevCapParm.dwReturn);
  if FCanPlay then Include(FCapabilities, mpCanPlay);

  DevCapParm.dwItem := mci_GetDevCaps_Device_Type;
  mciSendCommand(FDeviceID, mci_GetDevCaps, FFlags,  Longint(@DevCapParm) );
  devType := DevCapParm.dwReturn;
  if (devType = mci_DevType_Animation) or
     (devType = mci_DevType_Digital_Video) or
     (devType = mci_DevType_Overlay) or
     (devType = mci_DevType_VCR) then FCanStep := True;
  if FCanStep then Include(FCapabilities, mpCanStep);

  FFlags := mci_Anim_Where_Source;
  FError := mciSendCommand( FDeviceID, mci_Where, FFlags, Longint(@RectParms) );
  WorkR := RectParms.rc;
  FDWidth := WorkR.Right - WorkR.Left;
  FDHeight := WorkR.Bottom - WorkR.Top;
end; {GetDeviceCaps}

function TMediaPlayer.GetStart: Longint;
var
  StatusParm: TMCI_Status_Parms;
begin
  if not CheckIfOpen() then
  begin
    result:= 0;
    exit;
  end;
  FFlags := mci_Wait or mci_Status_Item or mci_Status_Start;
  StatusParm.dwItem := mci_Status_Position;
  FError := mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := StatusParm.dwReturn;
end;

function TMediaPlayer.GetLength: Longint;
var
  StatusParm: TMCI_Status_Parms;
begin
  if not CheckIfOpen() then
  begin
    result:= 0;
    exit;
  end;
  FFlags := mci_Wait or mci_Status_Item;
  StatusParm.dwItem := mci_Status_Length;
  FError := mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := StatusParm.dwReturn;
end;

function TMediaPlayer.GetTracks: Longint;
var
  StatusParm: TMCI_Status_Parms;
begin
  if not CheckIfOpen() then
  begin
    result:= 0;
    exit;
  end;
  FFlags := mci_Wait or mci_Status_Item;
  StatusParm.dwItem := mci_Status_Number_Of_Tracks;
  FError := mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := StatusParm.dwReturn;
end;

function TMediaPlayer.GetMode: TMPModes;
var
  StatusParm: TMCI_Status_Parms;
begin
  FFlags := mci_Wait or mci_Status_Item;
  StatusParm.dwItem := mci_Status_Mode;
  FError := mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := TMPModes(StatusParm.dwReturn - 524); {MCI Mode #s are 524+enum}
end;

function TMediaPlayer.GetPosition: Longint;
var
  StatusParm: TMCI_Status_Parms;
begin
  FFlags := mci_Wait or mci_Status_Item;
  StatusParm.dwItem := mci_Status_Position;
  FError := mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := StatusParm.dwReturn;
end;

function TMediaPlayer.GetTimeFormat: TMPTimeFormats;
var
  StatusParm: TMCI_Status_Parms;
begin
  if not CheckIfOpen() then
  begin
    result:= TMPTimeFormats(0);;
    exit;
  end;
  FFlags := mci_Wait or mci_Status_Item;
  StatusParm.dwItem := mci_Status_Time_Format;
  FError := mciSendCommand( FDeviceID, mci_Status, FFlags, Longint(@StatusParm));
  Result := TMPTimeFormats(StatusParm.dwReturn);
end;

function TMediaPlayer.GetErrorMessage: string;
var
  ErrMsg: array[0..4095] of Char;
begin
  if not mciGetErrorString(FError, ErrMsg, SizeOf(ErrMsg)) then
    Result := 'Unknown'
  else SetString(Result, ErrMsg, StrLen(ErrMsg));
end;

end.
