// CPlusPlusTests.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "map"
#include "functional"
#include "iostream"

using namespace std;

int main()
{
	typedef std::map<double, int, std::greater<double> > THRES;
	THRES threshM;

	for (int t = 0; t<10; t++)
	{
		threshM[rand()] = t;
	}

	for (THRES::iterator it = threshM.begin(); it != threshM.end(); ++it) {
		cout << (*it).second << "=" << (*it).first << "\n";
	}

	int t = threshM.begin()->second;
	cout << t;
	cin.get();

	return 0;
}

