# Robit
Robit is an AI Discord bot powered by [OpenAI's GPT-4](https://platform.openai.com/docs/models/gpt-4). His primary purpose is to be a little assistant within a discord server and to integrate seamlessly with the server. The bot is currently in beta and might break from time to time

## Add Robit to your server
You can add Robit by clicking [**here**](https://discord.com/api/oauth2/authorize?client_id=1049457745763500103&permissions=929860668416&scope=applications.commands%20bot)

# Features/Commands
## Conversation
You can freely chat with Robit by mentioning him in a message via @ or mention-reply

<details>
  <summary>Click for usage example</summary>

  ![RobitChat](https://github.com/TheRoboDoc/Robit/assets/18618265/3e54e8a0-14c4-42e3-b79f-5fdd38dfbb58)
  
</details>

## Generic Commands
A set of generic commands

### Ping
Pings the bot for a response

`/ping [optional]times:[integer][default:1][min:1][max:3]`

<details>
  <summary>Click for usage example</summary>

  ![RobitPing](https://github.com/TheRoboDoc/Robit/assets/18618265/d38f28a5-a1a9-4e00-8207-10b4fa1cd627)

</details>

### List Commands
Lists all the commands that the bot has

`/commands [optional]visible:[boolean][default:false]`

<details>
  <summary>Click for usage example</summary>

  ![RobitCommands](https://github.com/TheRoboDoc/Robit/assets/18618265/2cf2bf21-6ff8-48b9-a535-dc6c287e246d)

</details>

### Introduction
Bots introduction explains what he is

`/intro [optional]visible:[boolean][default:true]`

<details>
  <summary>Click for usage example</summary>

  ![RobitIntro](https://github.com/TheRoboDoc/Robit/assets/18618265/17b7d18f-27f5-4bb4-b92c-108438a127b2)

</details>

### Github
Posts a link to this GitHub page

`/github`

<details>
  <summary>Click for usage example</summary>

  ![RobitGithub](https://github.com/TheRoboDoc/Robit/assets/18618265/c556688b-4d13-48f7-9e0f-a2d59bf3b400)

</details>

## Auto response
You can automatic responses to content in messages

<details>
  <summary>Click for usage example</summary>

  ![RobitResponse](https://github.com/TheRoboDoc/Robit/assets/18618265/6ee32d2d-4203-480e-99a5-653f15ebc5be)
  
</details>

### Add
Adds an auto-response

`/response add name:[string] trigger:[string] response:[string] [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitResponseAdd](https://github.com/TheRoboDoc/Robit/assets/18618265/f6ef96ed-c56b-48cd-8a58-d54bfef35389)
  
</details>

### Remove
Removes an auto-response

`/response remove name:[string] [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitResponseRemove](https://github.com/TheRoboDoc/Robit/assets/18618265/2d1449a2-d30c-438d-b5ee-b47a57ee6430)
  
</details>

### Modify
Modifies an auto-response

`/response modify name:[string] trigger:[string] response:[string] [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitResponseModifiy](https://github.com/TheRoboDoc/Robit/assets/18618265/27abac36-dc08-4a0b-8815-b00d5a3b7463)
  
</details>

### List
Lists all auto-responses

`/response list [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitResponseList](https://github.com/TheRoboDoc/Robit/assets/18618265/d2af81ca-dd0f-4986-b7a9-9388579e8146)
  
</details>

### Ignore
Marks the channel to be ignored or not by Robit

`/response ignore:[boolean] [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitResponseIgnore](https://github.com/TheRoboDoc/Robit/assets/18618265/7752b6f1-0e68-4553-ba77-96f21c5de2ee)
  
</details>

## File Convert
Robit can convert media files. For example from png to jpg, or from mkv to mp4. Conversion from video to gif might return mixed results

`/convert media_file:[attachment] format:[enumerable] [optional]visible:[boolean][default:false]`

<details>
  <summary>Click for usage example</summary>

  ![RobitConvert](https://github.com/TheRoboDoc/Robit/assets/18618265/e07e0669-c775-4f3d-b558-d18ea8acca12)

</details>

### List of supported formats:
- mp4
- mov
- mkv
- webm
- gif
- jpg
- png
- tiff

## Tag Voice
Tag voice command mentions everyone in the same voice chat as you. For those cases where you want to really show your friends that one image of a cat

`/tagvoice [optional]message:[string][default:""] [optional]attachment:[attachment][default:null]`

**At least the message or attachment needs to have something in it as input*

<details>
  <summary>Click for usage example</summary>

  
  
</details>

## AI Prompt
A command to prompt Robit's AI module without context and in a longer form

`/prompt ai_prompt:[string] [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitPrompt](https://github.com/TheRoboDoc/Robit/assets/18618265/07f67921-f5b4-4cd0-a7f9-4903642a5c6f)

</details>

## AI Ignore
Makes Robit's AI module ignore the channel depending if ignore is set to `True` or `False`

`/ai_ignore ignore:[boolean] [optional]visible:[boolean]`

<details>
  <summary>Click for usage example</summary>

  ![RobitAIIgnore](https://github.com/TheRoboDoc/Robit/assets/18618265/340be34a-5ea0-4106-b1f6-4fd62452ca33)

</details>

## Random
A set of commands to generate random values

### Number Generation
Generates a number between minimal and maximal number

**Maximum value cannot be smaller than the minimal value*

`/random number maximum_value:[integer][min:0] [optional]minimal_value:[integer][default:0][min:0] [optional]visible:[boolean][default:true]`

<details>
  <summary>Click for usage example</summary>

  ![RobitRandomNumber](https://github.com/TheRoboDoc/Robit/assets/18618265/6ba8e554-6026-4ecf-9521-ad248768a4ea)

</details>

### Dice Roll
*"Rolls"* dice and displays a bunch of values as the result

`/random dice dice_type:[enumerable] [optional]amount:[integer][default:1][min:1][max:255] [optional]visible:[boolean][default:true]`

<details>
  <summary>Click for usage example</summary>

  ![RobitDice](https://github.com/TheRoboDoc/Robit/assets/18618265/1720ba36-ef93-4e2b-b312-2176b87519a0)

</details>

#### Dice Types
- D2 (Coin flip)
- D4 (Four-sided dice)
- D6 (Six-sided dice)
- D8 (Eight-sided dice)
- D10 (Ten-sided dice)
- D12 (Twelve-sided dice)
- D20 (Twenty-sided dice)


## Warhammer 40k Quotes
A set of commands to print out Warhammer 40k Imperium of Man's and its sub-faction quotes

### Selection Type
- First
- At Random

### By Author
Search for a quote by an in-universe author

`/wh40kquote by_author search:[string][max:40] [optional]result_type:[enumerable][default:at_random] [optional]count:[default:1][min:1][max:10] [optional]visible:[boolean][default:false]`

<details>
  <summary>Click for usage example</summary>

  ![RobitQuoteByAuthor](https://github.com/TheRoboDoc/Robit/assets/18618265/38af0e67-cbf1-4f6f-acd0-49d8511c5c35)

</details>

### By Source
Search for a quote by a real-life source

`/wh40kquote by_source search:[string][max:40] [optional]result_type:[enumerable][default:at_random] [optional]count:[default:1][min:1][max:10] [optional]visible:[boolean][default:false]`

<details>
  <summary>Click for usage example</summary>

  ![RobitQuoteBySource](https://github.com/TheRoboDoc/Robit/assets/18618265/d45fe84f-0e57-449b-bf08-d9e2cf7fc55a)

</details>

### Random
Gives out a random quote

`/wh40kquote random [optional]visible:[boolean][default:true]`

<details>
  <summary>Click for usage example</summary>

  ![RobitQuoteRandom](https://github.com/TheRoboDoc/Robit/assets/18618265/5fc71ab2-37eb-4f19-9563-31e540ca6a5b)

</details>

## Support
For support, you can visit [Robit's Little Shack Discord server](https://discord.gg/htxNBgAxZd)

## Tips

Help to cover Robit's hosting costs:

[<img src="https://cdn.discordapp.com/attachments/1096788658566209546/1121171401995472946/cup-borders.png"> Ko-Fi](https://ko-fi.com/robodoc)

[<img src="https://cdn.discordapp.com/attachments/1096788658566209546/1122794546678468678/paypal.png"> PayPal](https://www.paypal.com/donate/?hosted_button_id=XA4VRCET724AY)

## Credits
### Libraries
#### [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
Library to interact with Discord's API

#### [Betalgo's OpenAI library](https://github.com/betalgo/openai)
Library to interact with OpenAI's API

#### [Tim Miller's GiphyDotNet](https://github.com/drasticactions/GiphyDotNet)
Library to interact with Giphy's API

#### [Xabe FFmpeg](https://ffmpeg.xabe.net/index.html)
Library used to interact with [FFmpeg](https://ffmpeg.org/)

#### [Newtonsoft Json](https://www.newtonsoft.com/json)
Library to do JSON deserialization

### Art
#### Profile picture
Drawn by [Dia](https://twitter.com/Bubblekin123)

#### Loading Gif
Drawn by [Dia](https://twitter.com/Bubblekin123)

Animated by [me](https://twitter.com/The_RoboDoc)

#
*Robit runs on .NET 6.0*
