# AIDungeon-Extension
~~*Read this in other languages: [English](https://github.com/hisacat/AIDungeon-Extension/blob/master/README.md) [한국어](https://github.com/hisacat/AIDungeon-Extension/blob/master/README.ko.md) [日本語](https://github.com/hisacat/AIDungeon-Extension/blob/master/README.ja.md)*~~

<img width="100" alt="Screenshot_685" src="https://user-images.githubusercontent.com/17191898/105829172-5d393580-6007-11eb-88b5-96dd598bbb0f.png">

이것은 https://play.aidungeon.io 의 GUI 확장 런처로써, **번역 기능**을 포함한 추가 기능을 제공합니다.
현재 프로토타입 단계이며, 작업 현황은 [트렐로](https://trello.com/b/Y8P6VzhT)에서 확인 가능합니다.

<img width="700" src="https://user-images.githubusercontent.com/17191898/105830386-c7060f00-6008-11eb-9d04-ebee6844255c.png">

## 사전 준비
1. AIDungeon-Extension의 최신 [빌드](https://github.com/hisacat/AIDungeon-Extension/releases)
2. [Google Chrome](https://www.google.com/chrome)
3. 사용중인 Chrome 버전에 맞는 chromedriver를 [다운로드](https://chromedriver.chromium.org/downloads)합니다.
4. chromedriver.exe를 실행파일과 동일한 경로에 넣어주세요.

여기 Chrome 버전 확인 및 chromedriver 다운로드에 대한 [튜토리얼](https://github.com/hisacat/AIDungeon-Extension/blob/master/Documents/Install_chromedriver.md)이 있습니다.

## 플레이
프로그램을 실행하면 프로그램에 의해 조작되는 Chrome이 실행되며, 여기서 AIDungeon의 플레이가 감지되면, 게임 텍스트가 번역되어 출력됩니다. 또한 프로그램 내에서 커맨드를 입력하거나 버튼을 클릭할 수 있게 됩니다.

이는 기본적으로 AIDungeon과 비슷한 UX를 제공합니다.
* 인풋 텍스트 박스에서 /Do, /Say, /Story, /Redo, /Undo, /Retry 등의 커맨드 지원
* Redo, Undo, Retry 버튼

**현재 지원되지 않는 커맨드 : /Remember, /Alter 등 (지원 예정)**

또한, 텍스트의 디스플레이 방식은 기본적으로 AIDungeon의 "Play"모드와 유사한 환경을 가집니다.

## 지원 기능
*  게임 번역 기능   
시스템에 설정된 언어로 게임을 번역해줍니다.   
우측 설정에서 번역 대상 언어를 지정 가능합니다.
* 입력 텍스트 번역 기능   
Enter로 입력한 텍스트를 번역 가능합니다.   
**보내기는 Ctrl+Enter, 개행은 Shift-Enter입니다. (단축키 수정 지원 예정)**
* 스타일 기능   
우측 설정에서 폰트와 텍스트의 크기 및 색상, 배경 이미지 등을 지정 가능합니다.
<img width="700" src="https://user-images.githubusercontent.com/17191898/105832264-151c1200-600b-11eb-9154-a7922d6deda9.png" width=300>
* 자동 로그인 기능   
상단메뉴 "계정" 선택   
계정 정보는 MachineKey를 통해 암호화되어 로컬에 저장됩니다.
* 게임 내용을 텍스트 파일로 저장   
상단메뉴 "파일/저장" 선택 혹은 Ctrl+S

## 실험적인 기능
* 번역 사전 기능(실험적인 기능)   
이는 단순히 번역기에게 원문을 넘기기 전에, 특정 단어를 치환하는 기능입니다.   
결과에는 영문을 사용할 수 없으며, 번역의 질이 떨어지는 문제점이 있습니다.   
인명과 지명 등의 고유명사 정도만 작성하시는 것을 권장드립니다.   
우측 메뉴에서 번역 사전을 열어주세요.   
**"원문:번역"** 의 형태로 텍스트를 작성해주세요.(줄바꿈으로 구분)   
 **원문은 대소문자를 구분합니다.**   
수정이 완료되면, **번역 사전 업데이트** 버튼을 눌러주세요.   
	```
	번역 사전 텍스트 작성 예시:
	STR:힘
	INT:지능
	AGI:민첩
	...
	이 경우,"Your status: [STR] 10, [INT] 10, [AGI] 5" 라는 문장은
	"Your status: [힘] 10, [지능] 10, [민첩] 5"로 치환되어 번역기에게 전달됩니다.
	```

## 지원 예정
* 자동 업데이트 기능
* /Alter, /Revert 커맨드 지원
* 느린 타이핑 애니메이션 기능
* 여러 번역 엔진 지원 (파파고, yandex 등)
* 전체 화면 기능
* Prompt 및 선택지 지원
* 월드 인포 수정
* CLI(콘솔) 모드 지원

## 개발환경/사용된 라이브러리
C# WPF   
[Extended.Wpf.Toolkit](https://github.com/xceedsoftware/wpftoolkit) 4.0.2   
[HtmlAgilityPack](https://html-agility-pack.net/) 1.11.29   
[Newtonsoft.Json](https://www.newtonsoft.com/json) 12.0.3   
[Selenium](https://www.selenium.dev/) 3.7.0   

## 작동 방식
Chrome의 로그로부터 AIDungeon의 WebSocket 내용을 모니터링하여 데이터를 분석합니다. 텍스트 입력 등의 인풋 동작의 경우, XPath로 WebElement를 찾아 클릭이나 키를 보내는 등의 조작을 시뮬레이션합니다. 이로 인해 WebSocket 데이터의 변조는 일어나지 않습니다.

번역 기능의 경우, headless(창이 보여지지 않음)옵션으로 실행된 Chrome 클라이언트에서 Google translator의 번역 결과를 파싱합니다.

언제든 작동이 막힐 가능성이 있으며, 지속적인 업데이트를 진행할 생각입니다. 예를 들면...
> AIDungeon 웹의 구조 변경, WebSocket의 데이터 형식 변경, Google Translate웹의 구조 변경 등...

또한, 텍스트의 디스플레이 방식은 기본적으로 AIDungeon의 "Play"모드와 유사한 환경을 가집니다.


## 작동되지 않을 때

동작 중단의 주된 원인은 웹페이지의 구조변경으로 의해 XPath를 변경해주어야 할 경우입니다.   
문제가 발생하면 바로 업데이트 하려 하고 있습니다만, 일단 XPath를 직접 수정할 수 있도록 해두었습니다.   
각 요소의 XPath를 직접 찾을 수 있는 분이시라면, 가이드에 따라 아래 파일을 수정해주세요.    
프로그램과 같은 경로에서 "XPaths.txt'를 찾아 열어 수정해주세요.   
"\>"로 시작하는 라인은 키값이며, 이는 각 XPath를 구분짓기 위한 키값이니 수정하지 말아주세요.   
키값 아래에서부터 XPath를 추가/삭제해나갈 수 있으며, 개행문자로 구분합니다.   
여러 XPath가 존재할 경우, 가장 첫번째 라인의 XPath서부터 순차적으로 탐색합니다.   
```
>Login_IDInputBox
//*[@class="css-11aywtz r-snp9zz" and @type="email"]

>Login_PWInputBox
//*[@class="css-11aywtz r-snp9zz" and @type="password"]

>Login_LoginButton
//*[@aria-label="Login"]/div

>InputBox
//*[@class="css-1dbjc4n r-13awgt0"]/*[@class="css-1dbjc4n r-1p0dtai r-1d2f490 r-12vffkv r-u8s1d r-zchlnj r-ipm5af"]/*[@class="css-1dbjc4n r-1p0dtai r-1d2f490 r-12vffkv r-u8s1d r-zchlnj r-ipm5af" and not(@aria-hidden)]//*/textarea[@placeholder]

>PopupCloseButton
//*[@class="css-18t94o4 css-1dbjc4n r-1loqt21 r-u8s1d r-zchlnj r-ipm5af r-1otgn73 r-1i6wzkk r-lrvibr" and @aria-label="close"]
```

XPath 문제가 아닐 경우: 업데이트를 기다려주세요. 혹은 이슈 제기 부탁드립니다.

## 알려진 버그
기본적으로 가장 최신 텍스트부터 번역하도록 개발되어 있습니다만, Action이 많은 경우, 번역 순서가 꼬이는 경우가 있습니다. 원인은 파악했으며 현재 수정중에 있습니다.

## 면책 조항
이 프로그램을 사용함으로써 발생하는 모든 불이익에 대해서는 책임지지 않습니다.

## 후원
[Play.AIDungeon](https://play.aidungeon.io/)을 후원해주세요. 드래곤은 사랑입니다.

## 라이센스
MIT License
see [LICENSE](https://github.com/hisacat/AIDungeon-Extension/blob/master/LICENSE)

## Author
HisaCat [@hisacat](https://github.com/hisacat) ([twitter](https://twitter.com/ahisacat)/[blog](https://hisacat.tistory.com/))
