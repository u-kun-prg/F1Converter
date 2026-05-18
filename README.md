# F1Converter

## F1-Format

F1 Format は、古い実音源チップで楽曲再生が可能なデータフォーマットです。  
F1 Format　の詳細は、Document フォルダを参照してください。  

F1 Format is a data format that allows music to be played on older sound chips.  
Please check the Document for details.  

## F1Converter

![F1Converter](Images/F1Converter.png)

F1Converterは、F1フォーマットの、テキストの ".f1t"ファイルとバイナリの".f1"を相互変換が可能です。  
また、VGM フォーマット、s98 フォーマット、MDX フォーマットを、F1フォーマットに変換します。  

F1Converter can convert between F1 format text ".f1t" files and binary ".f1".  
It also converts VGM, s98 and MDX formats to F1 format.  

F1 Converterは、vgm, s98, mdxを変換する際に､  レジスタアクセスの削減によるデータの縮小化や、  
音源チップのマスタークロックに合わせた音程変換、簡単な音量変換、などのターゲットハードウェアに  
合わせたレジスタアクセスの制御を行うことができます。  

When converting vgm, s98, or mdx, the F1 Converter can reduce the amount of  
register access to reduce data size, convert pitch to match the master clock  
of the sound chip, perform simple volume changes, and control register access  
to suit the target hardware.


F1 Converterは、  
PCにシリアル接続されているArduino Uno R3、Arduino Mega 2560 R3 のフラッシュメモリに  
F1フォーマットファイルを、アップロードすることができます。  

The F1 Converter 
can upload F1 format files to the flash memory of an Arduino Uno R3  
or Arduino Mega 2560 R3 that is serially connected to a PC.



## Sound Chips.  

F1 Converterが、対応するサウンドチップ。  

Sound chips supported by F1 Converter.  
  
- YM2151(OPM)
- YM2203(OPN)
- YM2608(OPNA)
- YM2612(OPN2)
- YM2610(OPNB)
- YM2610B(OPNB2)
- YMF288(OPN3)
- YM3526(OPL)
- YM3812(OPL2)
- Y8950(MSXAUDIO)　実チップ未検証　Not verified on actual chip.
- YMF262(OPL4)
- YM2413(OPLL)
- SN76489(DCSG)
- AY_3_8910(PSG)
- YM2149(YPSG)
- K051649(KONAMI SCC)
- K052539(KONAMI SCCI)
- M6258(M6258)
- M6295(M6295)
- K053260(K053260)　実チップ未検証　Not verified on actual chip.

F1 フォーマットでは、チップ指定が存在しないため、対応チップは必要ありませんが、  
vgm、s98、mdx からの変換では、音程変換や音量変換のために、チップ指定が必要です。  

The F1 format does not require a corresponding chip because there is no chip specification,  
but when converting from vgm, s98, or mdx, a chip specification is required for pitch and  
volume conversion.



## F1 Hardware

F1フォーマットでは、ヘッダーにコマンドコードのバイト値を定義する必要があります。  
コマンドコードのバイト値は、サウンドチップごとに、未使用の値から割り当てます。  
チップに書き込むデータ側に、コマンドコードか、書き込むデータか、を識別するTopCode設定が  
可能ですが、F1フォーマットファイルのサイズが大きくなるため、やむをえない場合に限られます。    

In the F1 format, the command code byte value must be defined in the header.  
Command code byte values ​​are assigned from unused values ​​for each sound chip.  
It is possible to set TopCode on the data side to be written to the chip to distinguish between  
command codes and data to be written, but this is only used in unavoidable cases as it increases  
the size of the F1 format file.  

||Top<br>Code|End<br>Code|ChA1|ChCS|Lp|W1Byte|W2Byte|W1|W2|W3|W4|W5|W6|DacWr<br>1Byte|DacWr<br>RunLength|PCM<br>Seek|F0|F1|F2|F3|F4|
| :---: | :---: | :---:  | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
|YM2151(OPM)|None|00|02|03|04|05|06|07|09|0A|0B|0C|0D|00|00|00|00|00|00|00|00|
|YM2203(OPN)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2608(OPNA)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2612(OPN2)|None|40|4D|4E|4F|41|42|43|44|45|46|47|48|40|40|40|40|40|40|40|40|
|YM2610(OPNB)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2610B(OPNB2)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YMF288(OPN3L)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM3526(OPL)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM3812(OPL2)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YMF262(OPL3)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2413(OPLL)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2612(OPN2)<br>and<br>SN76489(DSCG)|None|40|4D|4E|4F|41|42|43|44|45|46|47|48|40|40|40|40|40|40|40|40|
|PSG(AY-3-8910)<br>and<br>K051649(SCC)|None|90|91|92|93|94|95|96|97|98|99|9A|9B|90|90|90|90|90|90|90|90|
|PSG(AY-3-8910)<br>and<br>K052539(SCCI)|None|90|91|92|93|94|95|96|97|98|99|9A|9B|90|90|90|90|90|90|90|90|
|PSG(AY-3-8910)<br>and<br>YM2413(OPLL)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2203(OPN)<br>x2(DUAL)|None|FF|FE|FD|FC|D0|D1|D2|D3|D4|D5|D6|D7|FF|FF|FF|FF|FF|FF|FF|FF|
|YM2151(OPM)<br>and<br>OKI-M6295|M6295-0E|00|02|03|04|05|06|07|09|0A|0B|0C|0D|00|00|00|00|00|00|00|00|
|YM2151(OPM)<br>and<br>KONAMI-K053260|K053260-0E|00|02|03|04|05|06|07|09|0A|0B|0C|0D|00|00|00|00|00|00|00|00|

### Hardwares 

ハードウェアは、Arduino Mega2560 Rev3 、Arduino Mega2560 Rev3 の  
シールドとして製作しています。Arduino の スケッチでF1フォーマットを再生します。  

The hardware is made as an Arduino Mega2560 Rev3 and a shield for  
Arduino Mega2560 Rev3. The Arduino sketch plays the F1 format.  

![Hardwares](Images/Hardwares.png)

ハードウェアの定義は、/Resources/target.xml で設定します。  
ハードウェアの回路図やスケッチは公開しません。  

The hardware definition is set in /Resources/target.xml.  
Hardware schematics and sketches will not be made public.  


## License

このプロジェクトはMITライセンスの下で公開されています。詳細はLICENSEファイルを参照してください。

This project is released under the MIT License, see the LICENSE file for details.

