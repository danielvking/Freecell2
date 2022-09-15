// HelloWorld.cpp
#include "HelloWorld.h"
#include <WinUser.h>

void HelloWorld::SayThis(wchar_t* phrase)
{
    MessageBox(NULL, phrase, L"Hello World Says", MB_OK);
}