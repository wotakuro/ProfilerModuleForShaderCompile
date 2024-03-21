# ProfilerModuleForShaderCompile


[English](README.md)<br />

## このツールについて
このツールは ShaderCompileをProfilerで一覧できるようにしているProfilerの拡張Moduleです。<br />
また一覧表示するだけでなく、そのShaderCompile情報から自動的にShaderVariantCollectionアセットを作成するツールです。<br />
<br />
こちらは、実機上で発生する ShaderCompileを見たいと言ったときに使えるツールです。<br />
<br />
姉妹ツール「[UnityShaderVariantLoggerForEditor](https://github.com/wotakuro/UnityShaderVariantLoggerForEditor) 」もあります。<br />
こちらはEditorプレイだけで、アプリ内でどのようなShaderCompileが走るのかを把握したいときに利用します。<br />

## 利用方法
### インストールについて
Packagesフォルダ以下にコチラの中身を入れてください

### 有効化方法について
![ScreenshotToUnityProfiler](Documentation~/EnableShaderCompileModule.png "How to enable")<br />
ProfilerのModuleに「ShaderCompile」があるので、コチラを有効にして利用します。

### 使い方

![ScreenshotToUnityProfiler](Documentation~/Screenshot.png "screenshote")<br />

#### 1.Target ShaderVariant Collection
こちらにShaderVariantCollectionアセットを指定すると、Profilerの情報を元に勝手にShaderVariantを足していきます。<br />
Enabledのチェックを外すと自動書き込み機能をオフにすることが出来ます。

#### 2.Advanced 
ここではLogファイルの書き出しに関する設定が出来ます。<br />
Logは自動的に Library/profilermodule.shadercompile/logs フォルダへ書き出されます。

#### 3.Counter Data
そのフレームで行われたShaderCompileの回数を出します。<br />
ただShaderCompileのカウンターが1フレーム遅れてカウントされてしまいます。<br />
そのため、1フレーム先の情報をActualとして出しています。<br />
参考情報までに実際のカウンターデータも出しています。

#### 4.ShaderCompileInformation

Profilerから見つけたShaderCompileを全て一覧にして出しています。<br />
ShowOnlyCurrentFrameのチェックを入れると現在のフレームの情報のみ出します。<br />
<br />
また"Export to csv"をする事で Profiler中にある全てのフレームのShaderCompile情報をCSVファイルに書き出すことが可能になっています。

