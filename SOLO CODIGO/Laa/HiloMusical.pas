unit HiloMusical;

interface

uses Classes, SyncObjs, Messages, MPlayerLite;

type
  TMusicThreadState = (mtStart, mtPlay, mtStop, mtEnd);
  TMusicThread = class(TThread)
  private
    wndHandle: THandle;
    waitEvent: TSimpleEvent;
    myState:TMusicThreadState;

    fVolume: integer;
    fFileName: string;
    fFilePath: string;
    FOnNextTrack: TNotifyEvent;
    FOnError: TGetStrProc;
    procedure WndProc(var Msg: TMessage);
  public
    constructor Create(filePath:string);
    destructor Destroy(); override;
    procedure play(fileName:string);
    procedure stop();
    procedure stopThread();
    procedure Execute(); override;
    property OnNextTrack: TNotifyEvent read FOnNextTrack write FOnNextTrack;
    property OnError: TGetStrProc read FOnError write FOnError;
  end;

implementation

uses Windows, Dialogs;


constructor TMusicThread.Create(filePath:string);
begin
  // Create suspended
  inherited Create(true);
  Self.FreeOnTerminate:= false;
  fFilePath:= filePath;
  fFileName:= '';
  fVolume:= 0;
  myState:= mtStart;
  waitEvent:= TSimpleEvent.Create();
  wndHandle:= Classes.AllocateHWnd(WndProc);
end;

destructor TMusicThread.Destroy();
begin
  Classes.DeallocateHWnd(wndHandle);
  waitEvent.free();
  inherited;
end;

procedure TMusicThread.WndProc(var Msg: TMessage);
const
  MM_MCINOTIFY = $3B9;
  mci_Notify_Successful = $1;
  mci_Notify_Superseded = $2;
  mci_Notify_Aborted = $4;
  mci_Notify_Failure = $8;
begin
  case Msg.Msg of
    MM_MCINOTIFY:
    if not Terminated then
    begin
      case Msg.WParam of
      mci_Notify_Successful: if assigned(FOnNextTrack) then FOnNextTrack(self);
      mci_Notify_Superseded:;
      mci_Notify_Aborted:;
      mci_Notify_Failure:;
      end;
    end;
    else Msg.Result := DefWindowProc(wndHandle, Msg.Msg, Msg.WParam, Msg.LParam);
  end;
end;

procedure TMusicThread.play(fileName:string);
begin
  if Terminated then exit;
  fFileName:= fileName;
  myState:= mtPlay;
  waitEvent.setEvent();
end;

procedure TMusicThread.stop();
begin
  if Terminated then exit;
  myState:= mtStop;
  waitEvent.setEvent();
end;

procedure TMusicThread.stopThread();
begin
  if Terminated then exit;
  myState:= mtEnd;
  waitEvent.setEvent();
end;

procedure TMusicThread.Execute();
var
  mediaPlayer: TMediaPlayer;
  waitResult: TWaitResult;

  procedure executeStateActions();
  begin
    case myState of
      mtStop:
      begin
        try
          mediaPlayer.Stop();
          mediaPlayer.Close();
        except
          if assigned(FOnError) then FOnError(mediaPlayer.ErrorMessage);
        end;
      end;
      mtPlay:
      begin
        try
          mediaPlayer.FileName:=fFilePath + fFileName;
          if (fFileName[length(fFileName)] = '3') then
            mediaPlayer.DeviceType := dtMp3
          else
            mediaPlayer.DeviceType := dtSequencer;
          mediaPlayer.Open();
          mediaPlayer.Play();
        except
          if assigned(FOnError) then FOnError(mediaPlayer.ErrorMessage);
        end;
      end;
      mtEnd:Terminate();
    end;
  end;
begin
  inherited;

  mediaPlayer:= TMediaPlayer.Create(wndHandle);
  repeat
    waitResult:=waitEvent.WaitFor(1000);
    if wrSignaled=waitResult then
    begin
      waitEvent.ResetEvent();
      executeStateActions();
    end;
  until Self.Terminated;
  mediaPlayer.Stop();
  mediaPlayer.Close();
  mediaPlayer.Free();
end;

end.
