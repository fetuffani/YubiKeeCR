# YubiKeeCR (beta)
This is a KeePass2 plugin intended to replace the [Keechallenge](https://github.com/brush701/keechallenge) plugin as it is no longer under development. 
The goal is to allow you to use your Yubikey in challenge-response mode in order to encrypt your database.
Some parts of the code was borrowed from the Keechallenge plugin, you may find some similarities like the YubiWrapper.

# Using the plugin
- Make sure your Yubikey (and the backup ones) is configured with your private HMAC secret. **Be sure that you do not lose your secret or the database will not be recoverable. Also be sure you have a backup Yubikey.**.
- Head to the [releases](https://github.com/fetuffani/YubiKeeCR/releases) page, download the latest zip file then unzip the contents in your [KeePass plugin folder](https://keepass.info/help/v2/plugins.html).
- Open your database security options (File -> Database Settings -> Security tab) and select the **YubiKeeCR** algorithm. 
- Save your database (be warned the database will not be compatible with other password managers as described below)

# How it works
In contrast to Keepassium, KeePassXC and KeePassDX, KeePass2 does not expose the database master seed to the plugin, so we cannot use it as the challenge to the Yubikey. Every time the database is saved, the plugin will generate a new challenge.

Since we need to store this challenge inside the database .kdbx file, the file format is slight different from the original specifications, rendering it not compatible with other password managers.

The plugin will encrypt your database with two AES keys, the first one is using the key provided by the Yubikey's response and the second one is provided by the KeePass2 application, mimicking the original AES implementation. This way the challenge is encrypted with your master key so it will not be exposed in plain-bytes inside the database file.

Also, this method does not need the HMAC secret to the known by the plugin as described by this [Keepassium article](https://keepassium.com/articles/keechallenge-for-yubikey/)

**WARNING**: If the master key is empty you will be relying only on the challenge-response method

# KDBX format changes

As described [here](KDBX%20File%20Format.md) ([original](https://gist.github.com/lgg/e6ccc6e212d18dd2ecd8a8c116fb1e45)) the database file is originally composed by two sections:
- First: Unencrypted header
- Second: The encrypted database XML

The plugin will add another section with the challenge instructions:
- First: Unencrypted header
- **Second: The challenge instructions (encrypted with the master key only)**
- Third: The encrypted database XML (encrypted with both master key and response key)