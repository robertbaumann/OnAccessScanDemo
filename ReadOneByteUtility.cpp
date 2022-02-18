// ReadOneByteUtility.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <chrono>
#include <fstream>
#include <iostream>
#include <thread>

const int delayBetweenMs = 500;

int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        std::cout << "Usage: " << argv[0] << " <filename to read>";
    }

    std::fstream fs;
    int retval = -1;

    try
    {
        char firstByte[2];

        fs.open(argv[1], std::fstream::in, std::fstream::binary);
        fs.read(firstByte, 1);
        retval = (fs || fs.eof()) ? 0 : -1;
        fs.close();

        // If file was readable once, then try again after brief delay.
        // This is to mitigate the 0.001 failure rate
        // on a single read of Eicar file.
        if (retval == 0)
        {
            std::chrono::milliseconds * delayMs = new std::chrono::milliseconds(delayBetweenMs);
            std::this_thread::sleep_for(*delayMs);

            fs.open(argv[1], std::fstream::in, std::fstream::binary);
            fs.read(firstByte, 1);
            retval = (fs || fs.eof()) ? 0 : -1;
            fs.close();
        }
    }
    catch(...)
    {
        retval = -1;
    }

    std::cout << "Returning " << retval << "\r\n";
    return retval;
    }

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file