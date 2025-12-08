(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)
unit GTimer;
interface
uses
  Classes, Windows;

const
  WM_GTIMER = $0400 {WM_USER} + $0011;

type
  TGTimer = class(TThread)
  private
    m_dueTime, m_Frequency, m_Interval: Int64;
    m_hwnd: HWND;
    m_fpts: integer;
    m_running: Boolean;
    m_enabled: Boolean;
    m_onSyncWait: TNotifyEvent;
    procedure SetFpts(value:integer);
    procedure SetEnabled(value:Boolean);
  public
    property OnSyncWait: TNotifyEvent write m_onSyncWait;
    constructor Create(aHwnd:HWND);
    procedure Execute(); override;
    property Enabled: Boolean read m_enabled write SetEnabled;
    property Fpts: integer read m_fpts write SetFpts;
  end;

implementation

uses mmsystem;

constructor TGTimer.Create(aHwnd:HWND);
begin
  // Create suspended
  inherited Create(true);
  m_enabled:= false;
  m_running:= false;
  m_hwnd:= aHwnd;
  QueryPerformanceFrequency(m_Frequency);
  m_fpts:= 0;
  fpts:= 1000;
  Self.FreeOnTerminate:= false;
end;

procedure TGTimer.SetEnabled(value:Boolean);
begin
  if m_enabled<>Value then
  begin
    m_enabled:= value;
    if (not m_running and value) then
    begin
      m_running:= true;
      resume();
    end;
  end;
end;

procedure TGTimer.SetFpts(value:integer);
begin
  if m_fpts<>Value then
  begin
    if Value<1000 then Value:=1000;
    if Value>1000000 then Value:=1000000;
    m_fpts:=Value;
    m_dueTime:=-10000000000 div value;
    m_Interval:= (m_Frequency * 1000) div value;
  end;
end;

procedure TGTimer.Execute();
var
  hTimer: Cardinal;
  lag, t, oldTime, dueTime: Int64;
begin
  inherited;

  dueTime:= m_dueTime;
  QueryPerformanceCounter(oldTime);

  timeBeginPeriod(1);
  hTimer:= CreateWaitableTimer(nil, TRUE, nil);
  repeat
    if not SetWaitableTimer(hTimer, dueTime, 0, nil, nil, FALSE) then break;
    if WaitForSingleObject(hTimer, INFINITE) = WAIT_OBJECT_0 then
      if m_enabled then
      begin
        if assigned(m_onSyncWait) then m_onSyncWait(self);

        postMessage(m_hWnd, WM_GTIMER, 0, 0);
        QueryPerformanceCounter(t);
        lag:= (t - oldTime) - m_Interval;
        oldTime:= t;

        if (lag >= m_Interval) then
          dueTime:= m_dueTime
        else
        begin
          inc(dueTime, (lag * 10000000 div m_Frequency));
          if (dueTime >= 0) then dueTime:= m_dueTime;
        end;
      end;
  until Self.Terminated;
  CloseHandle(hTimer);
  timeEndPeriod(1);
end;

end.

