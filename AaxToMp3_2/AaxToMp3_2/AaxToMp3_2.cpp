// AaxToMp3_2.cpp : Definiert den Einstiegspunkt für die Konsolenanwendung.
//

#include "stdafx.h"
#include <Windows.h>
#include <iostream>
#include <fstream>
#include <string>
#include <stdio.h>
#include <fcntl.h>
#include <io.h>
#include <wchar.h>
using namespace std;


typedef signed int __cdecl T_AAXOpenFileWinW(BYTE** paaxhandle, LPCWSTR fname);
typedef signed int __cdecl T_AAXCloseFile(BYTE* aaxhandle);
typedef int __cdecl T_AAXAuthenticateWin(BYTE *aaxhandle);
typedef signed int __cdecl T_AAXSeek(BYTE* aaxhandle, int);
typedef signed int __cdecl T_AAXGetAudioChannelCount(BYTE* aaxhandle, DWORD *channels);
typedef signed int __cdecl T_AAXGetSampleRate(BYTE *aaxhandle, DWORD *samplerate);
typedef signed int __cdecl T_AAXGetEncodedAudio(BYTE* aaxhandle, BYTE* buf, DWORD bufsize, DWORD* length);
typedef int __cdecl T_AAXDecodePCMFrame(BYTE* aaxhandle, BYTE* buf, DWORD bufsize, BYTE* decbuf, DWORD decbufsize, DWORD *length);

BYTE *aaxhandle; //no malloc needed!
DWORD encbufsize = 0x400;
DWORD decbufsize = 0x400 * 4; //probably 4KB would be enough <- Apparantly, it's the exact output size thing.
DWORD enclength = 0;
DWORD declength = 0;
BYTE *encbuf = (BYTE*) malloc(encbufsize);
BYTE *decbuf = (BYTE*) malloc(decbufsize);
DWORD channels = 0;
DWORD samplerate = 0;
int error = 0; //all AAX functions return 0 if no error

struct WaveHeader {
	BYTE RIFF[4]; // "RIFF"
	DWORD filesize; // filesize - 8
	BYTE WAVE[4]; // "WAVE"
	BYTE fmt[4]; // "fmt " (including space after fmt)
	DWORD data_hdrlen; // 16
	WORD fmt_type; // 1
	WORD num_channels; // AAXGetAudioChannelCount()
	DWORD sample_rate; // AAXGetSampleRate()
	DWORD bytes_per_second; // sample_rate * num_channels * 2
	WORD bytes_per_frame; // num_channels * 2
	WORD bits_per_sample; // 16
	BYTE DATA[4]; // "data"
	DWORD datasize;
};

std::string structToString (WaveHeader* theStruct)
{
  char* tempCstring = new char[sizeof(WaveHeader)+1];
  memcpy(tempCstring, theStruct, sizeof(WaveHeader));
  tempCstring[sizeof(WaveHeader)+1] = '0';
  std::string returnVal(tempCstring, sizeof(WaveHeader));
  //delete tempCstring;
  return returnVal;
}
int _tmain(int argc, wchar_t* argv[]) {
	LPCWSTR filepath=L"";
	if(argc == 3)
  	for(int i=0;i<argc;i++)
	{
		if(wcscmp(argv[i],L"-i")==0)
		{
			filepath = (LPCWSTR)(argv[i+1]);
			i++;
			continue;
		}
	}
	if(filepath==L"")
	{
		cout << "Invalid arguments" << endl;
		return -1;
	}
	//Load DLL
	HMODULE hDll = LoadLibrary(L"AAXSDKWin.dll");

	//Import methods from DLL
	T_AAXOpenFileWinW *AAXOpenFileWinW = (T_AAXOpenFileWinW*)GetProcAddress(hDll, "AAXOpenFileWinW");
	T_AAXCloseFile *AAXCloseFile = (T_AAXCloseFile*)GetProcAddress(hDll, "AAXCloseFile");
	T_AAXAuthenticateWin *AAXAuthenticateWin = (T_AAXAuthenticateWin*)GetProcAddress(hDll, "AAXAuthenticateWin");
	T_AAXSeek *AAXSeek = (T_AAXSeek*)GetProcAddress(hDll, "AAXSeek");
	T_AAXGetAudioChannelCount *AAXGetAudioChannelCount = (T_AAXGetAudioChannelCount*)GetProcAddress(hDll, "AAXGetAudioChannelCount");
	T_AAXGetSampleRate *AAXGetSampleRate = (T_AAXGetSampleRate*)GetProcAddress(hDll, "AAXGetSampleRate");
	T_AAXGetEncodedAudio *AAXGetEncodedAudio = (T_AAXGetEncodedAudio*)GetProcAddress(hDll, "AAXGetEncodedAudio");
	T_AAXDecodePCMFrame *AAXDecodePCMFrame = (T_AAXDecodePCMFrame*)GetProcAddress(hDll, "AAXDecodePCMFrame");

	//(AAXDecodePCMFrame() seems always to give 16 bit samples, so you can just take num_channels * 2 to calculate the bytes per frame and bytes per second)

	//Open AAX File
	//std::wstring str =std::wstring(filepath.begin(), filepath.end());
	error = AAXOpenFileWinW(&aaxhandle,filepath);
	error = AAXGetAudioChannelCount(aaxhandle, &channels);
	error = AAXGetSampleRate(aaxhandle, &samplerate);
	error = AAXSeek(aaxhandle, 0);
	error = AAXAuthenticateWin(aaxhandle); //without this call decoding will fail!

	//create some wav file here and write some dummy header to it (to reserve the space)
	cout.flush();
	fflush(stdout);
	_setmode(_fileno(stdout), _O_BINARY);

	DWORD filesize = 0;
	DWORD data_hdrlen = 16;
	WORD fmt_type = 1;
	WORD bits_per_sample = 16;
	DWORD datasize = 0;
	
	//Prepare wavefile header
	WaveHeader headerdata = {
		{ 'R', 'I', 'F', 'F' },		//BYTE	4
		filesize, //filesize - 8	//DWORD	4 - VARIABEL
		{ 'W', 'A', 'V', 'E' },		//BYTE	4
		{ 'f', 'm', 't', ' ' },		//BYTE	4
		data_hdrlen,				//DWORD	4
		fmt_type,					//WORD	2
		(WORD) channels,			//WORD	2
		samplerate,					//DWORD	4
		samplerate * channels * 2,	//DWORD	4
		(WORD) channels * 2,		//WORD	2
		bits_per_sample,			//WORD	2
		{ 'd', 'a', 't', 'a' },		//BYTE	4
		datasize					//DWORD	4 - VARIABEL
	};								//      44 Byte gesamt

	//Create pointer to headerdata and write data into file:
	DWORD headerdatasize = sizeof(struct WaveHeader);
	cout << structToString(&headerdata);

	DWORD datalength = 0;
	do {
		error = AAXGetEncodedAudio(aaxhandle, encbuf, encbufsize, &enclength);
		error = AAXDecodePCMFrame(aaxhandle, encbuf, enclength, decbuf, decbufsize, &declength);
		datalength += declength;
		//TODO: write decbuf to wav file here
		std::string s( reinterpret_cast<char const*>(decbuf), declength ) ;
		cout << s;
	} while (enclength > 0);

	error = AAXCloseFile(aaxhandle);
	return 0;
}