#include <ArduinoJson.h>
#include <ESP8266HTTPClient.h>
#include <ESP8266WiFi.h>
#include <ESP8266WiFiMulti.h>

// Wi-Fi接続パラメータ
const char* ssid = "xxxx";
const char* password = "xxxx";

const char* uri = "http://xxxx.azurewebsites.net/api/unkos";
    
// IOピン宣言
#define LEFT_BTN 14
#define RIGHT_BTN 12
#define ACTIVE 16
#define RBOOTED 13
#define LBOOTED 5
#define LEFT_RED_LED 15
#define RIGHT_RED_LED 4
#define LEFT_GREEN_LED 2
#define RIGHT_GREEN_LED 0

bool lBooted;
bool rBooted;
int batteryLevel;
HTTPClient http;
ESP8266WiFiMulti wifiMulti;
DynamicJsonBuffer jsonBuffer;

// 定形初期化処理（理解するまでは変更しないで）
void boot() {
  Serial.begin(74800);
  // pin setup
  pinMode(LBOOTED, INPUT_PULLUP);
  pinMode(RBOOTED, INPUT_PULLUP);
  pinMode(LEFT_RED_LED, OUTPUT);
  pinMode(RIGHT_RED_LED, OUTPUT);
  pinMode(LEFT_BTN, INPUT);
  pinMode(RIGHT_BTN, INPUT);

  // スリープから復帰するのに押されてたボタン状態を取得
  lBooted = digitalRead(LBOOTED) == LOW;
  rBooted = digitalRead(RBOOTED) == LOW;

  // アクティブモードへ移行
  digitalWrite(ACTIVE, LOW);
  pinMode(ACTIVE, OUTPUT);

  // ACTIVE==LOWのあとでしか緑LEDを使えない
  pinMode(LEFT_GREEN_LED, OUTPUT);
  pinMode(RIGHT_GREEN_LED, OUTPUT);

  // バッテリーレベル取得(アクティブモードでないとダメ)
  batteryLevel = analogRead(A0) * 3000 / 1024;  // [mV]

  // 電池が消耗していたり、通電のみだった場合はすぐにスリープする
  if (batteryLevel < 2300 || !lBooted && !rBooted) {
    ESP.deepSleep(0);
    delay(500);
  }
}

void setup() {
  boot();  // 必ずsetup関数の最初に実行

  if (lBooted) {
    digitalWrite(LEFT_GREEN_LED, HIGH);  // left green on
  }
  if (rBooted) {
    digitalWrite(RIGHT_GREEN_LED, HIGH);  // right green on
  }

  Serial.println();
  Serial.println("left: " + String(lBooted) + " right: " + String(rBooted));

  // Wi-Fi connect
  WiFi.mode(WIFI_STA);
  wifiMulti.addAP(ssid, password);
}

void loop() {
  if (wifiMulti.run() != WL_CONNECTED) {
    delay(500);
    return;
  }
  String message;
  if (lBooted && !rBooted) {
    message = "{ \"message\":\"うんこボタンの左を押した\" }";
  } else if (!lBooted && rBooted) {
    message = "{ \"message\":\"うんこボタンの右を押した\" }";
  } else if (lBooted && rBooted) {
    message = "{ \"message\":\"うんこボタンの両方を押した\" }";
  }
  if (message.length() == 0) {
    return;
  }

  Serial.println(uri);
  http.begin(uri);
  http.addHeader("Content-Type", "application/json");

  int code = http.POST(message);
  String content = http.getString();
  http.end();
  Serial.println(String(code));
  Serial.println(content);

  JsonObject& resp = jsonBuffer.parseObject(content);
  // タスクが終了したら必ずディープスリープすること。
  ESP.deepSleep(0);
  delay(500);
}
