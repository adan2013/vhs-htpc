#include <IRrecv.h>
#include <IRutils.h>
#include <RTClib.h>
#include <EEPROM.h>

const int irPin = 2;
const int dataPin = 14;
const int latchPin = 12;
const int clockPin = 13;
const int pcTriggerPin = 15;
const int pcStatePin = A0;

const int VOLTAGE_THRESHOLD = 512;
const int TRIGGER_INTERVAL = 500;
const int IR_DEBOUNCE_TIME = 200;

const byte COLON = B00010000;   // XOR with digits
const byte DP_DOT = B10000000;  // XOR with symbols

const byte DIGITS[] = {
  // 0000xxxx
  // NC NC C- C+ D1 D2 D3 D4
  // D1/2/3/4: 0-ON 1-OFF
  B00000111,  // D1
  B00001011,  // D2
  B00001101,  // D3
  B00001110,  // D4
};

enum Symbols {
  // 1xxxxxxx
  // DP A B C D E F G
  // 0-ON 1-OFF
  NUM_0 = B10000001,
  NUM_1 = B11001111,
  NUM_2 = B10010010,
  NUM_3 = B10000110,
  NUM_4 = B11001100,
  NUM_5 = B10100100,
  NUM_6 = B10100000,
  NUM_7 = B10001111,
  NUM_8 = B10000000,
  NUM_9 = B10000100,
  EMPTY = B11111111,
  DASH  = B11111110,
  DEG   = B10011100,
  ONE_LINE   = B11110111,
  TWO_LINE   = B11110110,
  THREE_LINE = B10110110,
  LETTER_C   = B10110001,
  LETTER_E   = B10110000,
  LETTER_F   = B10111000,
  LETTER_I   = B11111001,
  LETTER_M   = B10001001,
  LETTER_N   = B11101010,
  LETTER_O   = B10000001,
  LETTER_P   = B10011000,
  LETTER_R   = B11111010,
  LETTER_S   = B10100100,
  LETTER_T   = B11110000
};

enum IrCodes {
  // Sony remote control (soundbar)
  IR_01_INPUT = 0x4B0D,
  IR_02_POWER = 0x540C,
  IR_03_VERTICAL = 0x7E114,
  IR_04_CINEMA = 0x610D,
  IR_05_AUTOSOUND = 0xF4116,
  IR_06_MUSIC = 0x490D,
  IR_07_VOICE = 0x9C114,
  IR_08_NIGHT = 0x20D,
  IR_09_VOLUP = 0x240C,
  IR_10_VOLDN = 0x640C,
  IR_11_BASS = 0xDE114,
  IR_12_MUTE = 0x140C,
  IR_13_INDICATORS = 0x40D,
  IR_14_AUDIO = 0x48110,
  IR_15_GAME = 0x1E114,
  IR_16_NEWS = 0xDC116,
  IR_17_SPORTS = 0x18116,
  IR_18_STANDARD = 0xB8116,
  IR_19_AVSYNC = 0xE4114,
  IR_20_DTSDIALOG = 0x6C116
};

enum PcCommands {
  CMD_PROFILE = 1,
  CMD_MUTE = 2,
  CMD_FULLS = 3,
  CMD_RELOAD = 4,
  CMD_SW_TABS = 5,
  CMD_SW_WINDOWS = 6,
  CMD_UP = 7,
  CMD_DN = 8,
  CMD_ENTER = 9,
  CMD_PASTE = 10,
  CMD_S1 = 11,
  CMD_S2 = 12,
  CMD_S3 = 13,
  CMD_S4 = 14,
  CMD_S5 = 15,
  CMD_S6 = 16,
  CMD_APPS = 17,
  CMD_CLEAR = 18
};

enum Modes {
  INIT_TEST,
  MAIN_CLOCK,
  MAIN_TEMP,
  MAIN_SOFF,
  MAIN_SET_CLOCK,
  TRIGGERING_PC_1,
  TRIGGERING_PC_2,
  TRIGGERING_PC_3,
  WAITING_FOR_PC,
  ON_MSG,
  OFF_MSG,
  SET_HOURS,
  SET_MINUTES,
  MODE_MENU
};

String serialBuffer = "";
boolean serialDataIsReady = false;
int cpuTemp = 0;

bool reverseStartAnimation = false;
bool pcIsOn = false;
bool lastPcState = false;
unsigned long pcTriggerReleaseTime = 0;

RTC_DS1307 rtc;
int hours = 12;
int minutes = 0;
unsigned long nextClockRefreshTime = 0;

IRrecv irRecv(irPin);
decode_results results;
unsigned long lastCodeReceivedTime = 0;

Modes currentMode = INIT_TEST;
Modes currentDisplayMode = MAIN_CLOCK;
unsigned long lastModeSwitch = 0;

int activeSegment = 0;
bool activeColon = true;
byte currentSymbols[] = {
  B00000000,
  B00000000,
  B00000000,
  B00000000
};

void readSettingsFromEeprom() {
  unsigned int addr = 0;
  EEPROM.get(addr, currentDisplayMode);
  Serial.print("READ ");
  Serial.println(currentDisplayMode);
  switch (currentDisplayMode) {
    case MAIN_CLOCK:
    case MAIN_TEMP:
    case MAIN_SOFF:
      break;
    default:
      currentDisplayMode = MAIN_CLOCK;
  }
}

void saveSettingsToEeprom() {
  unsigned int addr = 0;
  EEPROM.put(addr, currentDisplayMode);
  EEPROM.commit();
  Serial.print("WRITE ");
  Serial.println(currentDisplayMode);
}

void displaySymbol(int pos, Symbols segments, bool dp = false) {
  if (pos >= 0 && pos < 4) {
    byte val = segments;
    if (dp) val ^= DP_DOT;
    currentSymbols[pos] = val;
  }
}

void displayNumber(int pos, int num, bool dp = false) {
  Symbols s = NUM_0;
  switch (num) {
    case 0: s = NUM_0; break;
    case 1: s = NUM_1; break;
    case 2: s = NUM_2; break;
    case 3: s = NUM_3; break;
    case 4: s = NUM_4; break;
    case 5: s = NUM_5; break;
    case 6: s = NUM_6; break;
    case 7: s = NUM_7; break;
    case 8: s = NUM_8; break;
    case 9: s = NUM_9; break;
  }
  displaySymbol(pos, s, dp);
}

void turnOnColon() {
  activeColon = true;
}

void turnOffColon() {
  activeColon = false;
}

void turnOnAllSegments() {
  displaySymbol(0, NUM_8, true);
  displaySymbol(1, NUM_8, true);
  displaySymbol(2, NUM_8, true);
  displaySymbol(3, NUM_8, true);
  turnOnColon();
}

void turnOffScreen() {
  displaySymbol(0, EMPTY, false);
  displaySymbol(1, EMPTY, false);
  displaySymbol(2, EMPTY, false);
  displaySymbol(3, EMPTY, false);
  turnOffColon();
}

void refreshScreen() {
  digitalWrite(latchPin, LOW);
  byte digitsRegistry = DIGITS[activeSegment];
  if(activeColon) digitsRegistry ^= COLON;
  shiftOut(dataPin, clockPin, LSBFIRST, digitsRegistry);
  byte symbolsRegistry = currentSymbols[activeSegment];
  if(activeSegment == 3 && lastCodeReceivedTime > 0) {
    symbolsRegistry ^= DP_DOT;
    if(millis() - lastCodeReceivedTime >= IR_DEBOUNCE_TIME) lastCodeReceivedTime = 0;
  }
  shiftOut(dataPin, clockPin, LSBFIRST, symbolsRegistry);
  digitalWrite(latchPin, HIGH);
  activeSegment++;
  if(activeSegment > 3) activeSegment = 0;
}

bool isOnMainScreen() {
  return currentMode == currentDisplayMode || currentMode == WAITING_FOR_PC;
}

void switchMode(Modes mode) {
  lastModeSwitch = millis();
  currentMode = mode;
  turnOffColon();
  switch (currentMode) {
    case INIT_TEST:
      turnOnAllSegments();
      break;
    case MAIN_CLOCK:
    case MAIN_TEMP:
      if (currentMode == MAIN_TEMP && pcIsOn) {
        if (cpuTemp > 0) {
          displayNumber(0, cpuTemp / 10);
          displayNumber(1, cpuTemp % 10);
        } else {
          displaySymbol(0, DASH);
          displaySymbol(1, DASH);
        }
        displaySymbol(2, DEG);
        displaySymbol(3, LETTER_C);
      } else {
        displayNumber(0, hours / 10);
        displayNumber(1, hours % 10);
        displayNumber(2, minutes / 10);
        displayNumber(3, minutes % 10);
        turnOnColon();
      }
      break;
    case MAIN_SOFF:
      turnOffScreen();
      break;
    case TRIGGERING_PC_1:
      displaySymbol(0, ONE_LINE);
      displaySymbol(1, ONE_LINE);
      displaySymbol(2, ONE_LINE);
      displaySymbol(3, ONE_LINE);
      break;
    case TRIGGERING_PC_2:
      displaySymbol(0, TWO_LINE);
      displaySymbol(1, TWO_LINE);
      displaySymbol(2, TWO_LINE);
      displaySymbol(3, TWO_LINE);
      break;
    case TRIGGERING_PC_3:
      displaySymbol(0, THREE_LINE);
      displaySymbol(1, THREE_LINE);
      displaySymbol(2, THREE_LINE);
      displaySymbol(3, THREE_LINE);
      break;
    case WAITING_FOR_PC:
      turnOffScreen();
      break;
    case ON_MSG:
      displaySymbol(0, LETTER_O);
      displaySymbol(1, LETTER_N);
      displaySymbol(2, EMPTY);
      displaySymbol(3, EMPTY);
      break;
    case OFF_MSG:
      displaySymbol(0, LETTER_O);
      displaySymbol(1, LETTER_F);
      displaySymbol(2, LETTER_F);
      displaySymbol(3, EMPTY);
      break;
    case SET_HOURS:
      displayNumber(0, hours / 10);
      displayNumber(1, hours % 10);
      displaySymbol(2, DASH);
      displaySymbol(3, DASH);
      break;
    case SET_MINUTES:
      displaySymbol(0, DASH);
      displaySymbol(1, DASH);
      displayNumber(2, minutes / 10);
      displayNumber(3, minutes % 10);
      break;
    case MODE_MENU:
      switch (currentDisplayMode) {
        case MAIN_CLOCK:
          displaySymbol(0, LETTER_T);
          displaySymbol(1, LETTER_I);
          displaySymbol(2, LETTER_M);
          displaySymbol(3, LETTER_E);
          break;
        case MAIN_TEMP:
          displaySymbol(0, LETTER_T);
          displaySymbol(1, LETTER_E);
          displaySymbol(2, LETTER_M);
          displaySymbol(3, LETTER_P);
          break;
        case MAIN_SOFF:
          displaySymbol(0, LETTER_S);
          displaySymbol(1, LETTER_O);
          displaySymbol(2, LETTER_F);
          displaySymbol(3, LETTER_F);
          break;
        case MAIN_SET_CLOCK:
          displaySymbol(0, LETTER_S);
          displaySymbol(1, LETTER_E);
          displaySymbol(2, LETTER_T);
          displaySymbol(3, LETTER_C);
          break;
        default:
          displaySymbol(0, DASH);
          displaySymbol(1, DASH);
          displaySymbol(2, DASH);
          displaySymbol(3, DASH);
      }
      break;
    default:
      displaySymbol(0, LETTER_E);
      displaySymbol(1, LETTER_R);
      displaySymbol(2, LETTER_R);
      displaySymbol(3, EMPTY);
  }
}

void autoSwitchMode() {
  int timeout = 0;
  switch (currentMode) {
    case INIT_TEST: timeout = pcIsOn ? 1 : 2000; break;
    case TRIGGERING_PC_1:
    case TRIGGERING_PC_2:
    case TRIGGERING_PC_3:
      timeout = 200;
      break;
    case WAITING_FOR_PC: timeout = pcIsOn ? 800 : 2200; break;
    case ON_MSG: timeout = 3000; break;
    case OFF_MSG: timeout = 3000; break;
    case SET_HOURS: timeout = 6000; break;
    case SET_MINUTES: timeout = 6000; break;
    case MODE_MENU: timeout = 3000; break;
  }
  if (timeout > 0 && millis() - lastModeSwitch >= timeout) {
    switch (currentMode) {
      case TRIGGERING_PC_1:
        switchMode(reverseStartAnimation ? WAITING_FOR_PC : TRIGGERING_PC_2);
        if(reverseStartAnimation) {
          pcTriggerReleaseTime = millis() + TRIGGER_INTERVAL;
          digitalWrite(pcTriggerPin, HIGH);
        }
        break;
      case TRIGGERING_PC_2:
        switchMode(reverseStartAnimation ? TRIGGERING_PC_1 : TRIGGERING_PC_3);
        break;
      case TRIGGERING_PC_3:
        reverseStartAnimation = true;
        switchMode(TRIGGERING_PC_2);
        break;
      case SET_HOURS:
        switchMode(SET_MINUTES);
        break;
      case MODE_MENU:
        if (currentDisplayMode == MAIN_SET_CLOCK) {
          currentDisplayMode = MAIN_CLOCK;
          switchMode(SET_HOURS);
        } else {
          switchMode(currentDisplayMode);
        }
        saveSettingsToEeprom();
        break;
      default:
        switchMode(currentDisplayMode);
    }
  }
}

void sendCommand(PcCommands cmd) {
  Serial.print("B");
  Serial.print(cmd, DEC);
  Serial.println();
}

void onVolumeButtonsClick(int diff) {
  if(currentMode != SET_HOURS && currentMode != SET_MINUTES) {
    sendCommand(diff > 0 ? CMD_UP : CMD_DN);
  }else{
    if(currentMode == SET_HOURS) {
      hours += diff;
      if(hours < 0) hours = 23;
      if(hours > 23) hours = 0;
    }
    if(currentMode == SET_MINUTES) {
      minutes += diff;
      if(minutes < 0) minutes = 59;
      if(minutes > 59) minutes = 0;
    }
    switchMode(currentMode);
    rtc.adjust(DateTime(1970, 1, 1, hours, minutes, 0));
  }
}

void monitorPcState() {
  pcIsOn = analogRead(pcStatePin) < VOLTAGE_THRESHOLD;
  if (pcIsOn != lastPcState) {
    cpuTemp = 0;
    if (isOnMainScreen()) switchMode(pcIsOn ? ON_MSG : OFF_MSG);
  }
  lastPcState = pcIsOn;
  if(pcTriggerReleaseTime > 0 && millis() > pcTriggerReleaseTime) {
    pcTriggerReleaseTime = 0;
    digitalWrite(pcTriggerPin, LOW);
  }
}

void readIrCodes() {
  if(irRecv.decode(&results)) {
    if(lastCodeReceivedTime == 0) {
      bool codeIsCorrect = true;
      switch(results.value) {
        case IR_01_INPUT: // mode menu
          switch(currentMode) {
            case MODE_MENU:
              switch(currentDisplayMode) {
                case MAIN_CLOCK: currentDisplayMode = MAIN_TEMP; break;
                case MAIN_TEMP: currentDisplayMode = MAIN_SOFF; break;
                case MAIN_SOFF: currentDisplayMode = MAIN_SET_CLOCK; break;
                default: currentDisplayMode = MAIN_CLOCK;
              }
              switchMode(MODE_MENU);
              break;
            case SET_HOURS:
              switchMode(SET_MINUTES);
              break;
            case SET_MINUTES:
              switchMode(currentDisplayMode);
              break;
            default:
              switchMode(MODE_MENU);
          }
          break;
        case IR_02_POWER: // on/off the pc
          if(isOnMainScreen()) {
            reverseStartAnimation = false;
            switchMode(TRIGGERING_PC_1);
          }
          break;
        case IR_03_VERTICAL: // profile switch
          sendCommand(CMD_PROFILE);
          break;
        case IR_04_CINEMA: // mute sound
          sendCommand(CMD_MUTE);
          break;
        case IR_05_AUTOSOUND: // full screen
          sendCommand(CMD_FULLS);
          break;
        case IR_06_MUSIC: // reload view
          sendCommand(CMD_RELOAD);
          break;
        case IR_07_VOICE: // switch tabs
          sendCommand(CMD_SW_TABS);
          break;
        case IR_08_NIGHT: // switch windows
          sendCommand(CMD_SW_WINDOWS);
          break;
        case IR_09_VOLUP: // volume/value up
          onVolumeButtonsClick(1);
          break;
        case IR_10_VOLDN: // volume/value down
          onVolumeButtonsClick(-1);
          break;
        case IR_11_BASS: // enter/select
          sendCommand(CMD_ENTER);
          break;
        case IR_12_MUTE: // paste from clipboard
          sendCommand(CMD_PASTE);
          break;
        case IR_13_INDICATORS: // slot 1
          sendCommand(CMD_S1);
          break;
        case IR_14_AUDIO: // slot 2
          sendCommand(CMD_S2);
          break;
        case IR_15_GAME: // slot 3
          sendCommand(CMD_S3);
          break;
        case IR_16_NEWS: // slot 4
          sendCommand(CMD_S4);
          break;
        case IR_17_SPORTS: // slot 5
          sendCommand(CMD_S5);
          break;
        case IR_18_STANDARD: // slot 6
          sendCommand(CMD_S6);
          break;
        case IR_19_AVSYNC: // apps menu
          sendCommand(CMD_APPS);
          break;
        case IR_20_DTSDIALOG: // clear text field
          sendCommand(CMD_CLEAR);
          break;
        default:
          codeIsCorrect = false;
      }
      if(codeIsCorrect) lastCodeReceivedTime = millis();
    }
    irRecv.resume();
  }
}

void updateTimeOnScreen() {
  DateTime now = rtc.now();
  hours = now.hour();
  minutes = now.minute();
  nextClockRefreshTime = millis() + (61 - now.second()) * 1000;
  if(currentMode == MAIN_CLOCK || (currentMode == MAIN_TEMP && !pcIsOn)) {
    displayNumber(0, hours / 10);
    displayNumber(1, hours % 10);
    displayNumber(2, minutes / 10);
    displayNumber(3, minutes % 10);
  }
}

void setup() {
  EEPROM.begin(8);
  Serial.begin(115200);
  while (!Serial) delay(50);
  while (!rtc.begin()) delay(50);
  if(!rtc.isrunning()) {
    rtc.adjust(DateTime(1970, 1, 1, 12, 0, 0));
    Serial.println("RTC time set to 1970/1/1 12:00:00");
  }
  irRecv.enableIRIn();
  pinMode(dataPin, OUTPUT);
  pinMode(latchPin, OUTPUT);
  pinMode(clockPin, OUTPUT);
  pinMode(pcTriggerPin, OUTPUT);
  Serial.println("System initialized");
  readSettingsFromEeprom();
  monitorPcState();
  switchMode(INIT_TEST);
  updateTimeOnScreen();
}

void loop() {
  if(millis() > nextClockRefreshTime) updateTimeOnScreen();
  if(serialDataIsReady) {
    String cmdType = serialBuffer.substring(0, 1);
    String cmdData = serialBuffer.substring(1);
    if(cmdType == "T") {
      cpuTemp = cmdData.toInt();
      if(currentMode == MAIN_TEMP && pcIsOn) {
        switchMode(MAIN_TEMP);
      }
    }
    serialDataIsReady = false;
    serialBuffer = "";
  }
  monitorPcState();
  readIrCodes();
  autoSwitchMode();
  refreshScreen();
}

void serialEvent() {
  while(Serial.available() > 0) {
    char inChar = (char)Serial.read();
    if(inChar == '\n') {
      serialDataIsReady = true;
      break;
    }else{
      serialBuffer += inChar;
    }
  }
}
