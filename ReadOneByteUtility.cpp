// ReadOneByteUtility.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <fstream>

int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        std::cout << "Usage: " << argv[0] << " <filename to read>";
    }

    try
    {
        std::fstream fs;
        fs.open(argv[1], std::fstream::in, std::fstream::binary);

        char firstByte[2];
        fs.read(firstByte, 1);
        if (!fs)
        {
            std::cout << "Returning -1" << "\r\n";
            fs.close();
            return -1;
        }
        fs.close();
        std::cout << "Returning 0" << "\r\n";
        return 0;
    }
    catch(...)
    {
        std::cout << "Returning -1" << "\r\n";
        return -1;
    }
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
