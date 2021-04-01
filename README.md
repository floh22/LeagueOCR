<!--
*** Thanks for checking out the Best-README-Template. If you have a suggestion
*** that would make this better, please fork the LeagueOCR and create a pull request
*** or simply open an issue with the tag "enhancement".
*** Thanks again! Now go create something AMAZING! :D
***
***
***
*** To avoid retyping too much info. Do a search and replace for the following:
*** floh22, LeagueOCR, @larseble, email, LeagueOCR, OCR Analysis on League of Legends spectator games to augment the official Riot Games API
-->



<!-- PROJECT SHIELDS -->
<!--
*** I'm using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links
-->
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]



<!-- PROJECT LOGO -->
<br />
<p align="center">
  <h3 align="center">LeagueOCR</h3>

  <p align="center">
    OCR Analysis on League of Legends spectator games to augment the official Riot Games API
    <br />
    <a href="https://github.com/floh22/LeagueOCR"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/floh22/LeagueOCR">View Demo</a>
    ·
    <a href="https://github.com/floh22/LeagueOCR/issues">Report Bug</a>
    ·
    <a href="https://github.com/floh22/LeagueOCR/issues">Request Feature</a>
  </p>
</p>

<!-- ABOUT THE PROJECT -->
## About The Project

OCR Analysis on League of Legends spectator games to augment the official Riot Games API

<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these steps.

### Prerequisites


* .NET Framework 4.7.2
* Windows 10 20H1 (May 2020 Update) Build 19041

### League Settings

* Max UI Scale (100)
* Both default and eSports timers supported
* 16:9 Resolution (Native 1080p but higher should work well)

### Installation

1. Download [latest release](https://github.com/floh22/LeagueOCR/releases/tag/v0.4.3)
2. Unzip folder
3. Run LeagueOCR.exe



<!-- USAGE EXAMPLES -->
## Usage

Augment the Riot Games League of Legends API during spectator matches. Useful for stream overlays or real time game analysis when applications cant be installed on player computers to get this data through the game API directly.

Two endpoints are exposed at the moment.

http://localhost:3002/api/objectives

	-> http://localhost:3002/api/objectives/Dragon
	-> http://localhost:3002/api/objectives/Baron

http://localhost:3002/api/teams

	-> http://localhost:3002/api/teams/(0/ORDER)
	-> http://localhost:3002/api/teams/(1/CHAOS)

Objectives:
   ```sh
	[{
		"Type":"mountain",
		"Cooldown":267,
		"IsAlive":false,
		"TimesTakenInMatch":3,
		"LastTakenBy":0,
		"FoundTeam":true,
		"TimeSinceTaken":3
	},
	{
		"Type":"Baron",
		"Cooldown":219,
		"IsAlive":false,
		"TimesTakenInMatch":1,
		"LastTakenBy":1,
		"FoundTeam":false,
		"TimeSinceTaken":110
	}]
   ```
Teams:
   ```sh
    [{
		"Id":0,
		"TeamName":"ORDER",
		"Gold":2500
    },
	{
		"Id":1,
		"TeamName":"CHAOS",
		"Gold":2500
	}]
   ```

If you build a project using this, feel free to let me know!


<!-- ROADMAP -->
## Roadmap

See the [open issues](https://github.com/floh22/LeagueOCR/issues) for a list of proposed features (and known issues).



<!-- CONTRIBUTING -->
## Contributing

Any contributions you make are **greatly appreciated**. I am by no means an expert at .NET development and this project is somewhat of a mess in parts.

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.



<!-- CONTACT -->
## Contact

Lars Eble - [@larseble](https://twitter.com/@larseble)

Project Link: [https://github.com/floh22/LeagueOCR](https://github.com/floh22/LeagueOCR)






<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/floh22/LeagueOCR.svg?style=for-the-badge
[contributors-url]: https://github.com/floh22/LeagueOCR/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/floh22/LeagueOCR.svg?style=for-the-badge
[forks-url]: https://github.com/floh22/LeagueOCR/network/members
[stars-shield]: https://img.shields.io/github/stars/floh22/LeagueOCR.svg?style=for-the-badge
[stars-url]: https://github.com/floh22/LeagueOCR/stargazers
[issues-shield]: https://img.shields.io/github/issues/floh22/LeagueOCR.svg?style=for-the-badge
[issues-url]: https://github.com/floh22/LeagueOCR/issues
[license-shield]: https://img.shields.io/github/license/floh22/LeagueOCR.svg?style=for-the-badge
[license-url]: https://github.com/floh22/LeagueOCR/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/floh22
