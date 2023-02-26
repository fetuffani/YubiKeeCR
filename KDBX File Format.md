* [Original article](http://blog.sharedmemory.fr/en/2014/04/30/keepass-file-format-explained/)
* [Wayback machine](http://web.archive.org/web/20160229111603/http://blog.sharedmemory.fr/en/2014/04/30/keepass-file-format-explained/)

---

# Keepass file format explained

I’m currently working (I’m just at the beginning, and I’m quite slow) on a personal project that will use Keepass files (kdb and kdbx).  
I tried to find some documentation about .kdb and .kdbx format, but I didn’t find anything, even in the Keepass official website. I you want to know how these file formats are structured, you must read Keepass’s source code. So I wrote this article that explains how Keepass file format are structured, maybe it will help someone.

This article will explained the different fields and headers of kdb and kdbx file formats.

A .kdb or .kdbx file is composed of 2 things :

*   a header (not encrypted) containing various informations on how to decrypt the database, and informations on the database.
*   the database, in XML and encrypted (normally).

The header start for both file formats by two 4 bytes fields : the file signatures.

## File Signature 1 (4 bytes) & File Signature 2 (4 bytes)

.kdb and .kdbx file formats’ header first have 2 fields of 4 bytes each that are the file signatures (cf KdbxFile.cs of Keepass2 source code).

File Signature 1 (the first field) will always have a value of 0x9AA2D903 .

File Signature 2 (the second field) can have (for now) 3 different value, each value indicating the file format/version :

*   for .kdb files (KeePass 1.x file format) : 0xB54BFB65 ,
*   for kdbx file of KeePass 2.x pre-release (alpha & beta) : 0xB54BFB66 ,
*   for kdbx file of KeePass post-release : 0xB54BFB67 .

After these 2 fields, .kdb and .kdbx differ totally : .kdb has fixed number of fields taking a fixed number of bytes in its header, while .kdbx has a TLV list of fields in its header.

## .kdb header’s fields

Here’s the ordered list of the different fields of the header of a .kdb file, their size and their meaning :

1.  (4 bytes) KeePass Flags : contains the flag indicating the cipher used for the database (2 is for AES, 4 is for ARC4 , 8 is for Twofish, but only AES and Twofish seems to be used by KeePass). KeePass also declares a define of 1 for SHA2 that would be put in this field, but it doesn’t seem to be used ,
2.  (4 bytes) KeePass Version : it looks like each version of KeePass save the database with a different value for this field, but I don’t really understand their weird shenanigans about it. sorry,
3.  (16 bytes) Master Seed : Seed that get concatenated then hashed with the transformed master key (see later how to) to form the final master key,
4.  (16 bytes) Encryption IV : the IV used for content encryption,
5.  (4 bytes) Number of Groups : contains the total number of groups in the database,
6.  (4 bytes) Number of Entries : contains the total number of entries in the database,
7.  (32 bytes) Contents Hash : a SHA-256 hash of the database, used for integrity checking,
8.  (32 bytes) Transform Seed : The key used as seed for AES to generate the transformed master key from the user master key. The generation of the transformed master key consists in encrypting the user master key N rounds in total, N being the value of the field Key Encrypt Rounds fields value,
9.  (4 bytes) Key Encrypt Rounds : number of rounds the user master key must be encrypted to generate the transformed master key.

## .kdbx header’s fields

.kdbx header’s fields are a [Type-Length-Value](http://web.archive.org/web/20160229111603/http://en.wikipedia.org/wiki/Type-length-value "TLV description") list.

The Type takes 1 byte, the Length takes 2 bytes, and the Value takes Length bytes. Here’s a list of all Types (their integer code, and name) that can be put in the header (for now), their meaning and the “normal” length (the one that you can expect to get, according to KeePass source code, but it could be different, so you should still use the Length field to determine how many bytes you have to read for the Value field) in this format : “Integer_code = (size) Name : Description” :

*   0 = (0 bytes) End of Header : indicates that there is no more field to read before the database part (you still have to read 2 bytes for the Length field of TLV),
*   1 = (? bytes) Comment : this field doesn’t seem to be used in KeePass source code, but you may encounter it. Maybe its value is an ASCII string.
*   2 = (16 bytes) Cipher ID : the UUID identifying the cipher. KeePass 2 has an implementation allowing the use of other cipher, but only AES with a UUID of [0x31, 0xC1, 0xF2, 0xE6, 0xBF, 0x71, 0x43, 0x50, 0xBE, 0x58, 0x05, 0x21, 0x6A, 0xFC, 0x5A, 0xFF] is implemented in KeePass 2 for now.
*   3 = (4 bytes) Compression Flags : give the compression algorithm used to compress the database. For now only 2 possible value can be set : None (Value : 0) and GZip (Value : 1). For now, the Value of this field should not be greater or equal to 2\. The 4 bytes of the Value should be convert to a 32 bit signed integer before comparing it to known values,
*   4 = (16 bytes) Master Seed : The salt that will be concatenated to the transformed master key then hashed, to create the final master key,
*   5 = (32 bytes) Transform Seed : The key used by AES as seed to generate the transformed master key,
*   6 = (8 bytes ) Transform Rounds : The number of Rounds you have to compute the transformed master key,
*   7 = (? bytes ) Encryption IV : The IV used by the cipher that encrypted the database,
*   8 = (? bytes ) Protected Stream Key : The key/seed for the cipher used to encrypt the password of an entry in the database (see later),
*   9 = (32 bytes) Stream Start Bytes, indicates the first 32 unencrypted bytes of the database part of the file (to check if the file is corrupt, or the key correct,etc). These 32 bytes should have been randomly generated when the file was saved. Length should be 32 bytes ,
*   10= (4 bytes) Inner Random Stream ID : the ID of the cipher used to encrypted the password of an entry in the database (see later), for now you can expect to have : 0 for nothing, 1 for ARC4, 2 for Salsa20.

## How to generate the final master key

First, you have to generate the composite key, then transform it to generate the transformed key, then finally use this transformed key to generate the final master key.

### The composite key

The way to generate the composite key differs from KeePass 1 to KeePass 2 because you can use different credentials in those two : in KeePass 1, you can encrypt the database with a passphrase and/or a keyfile (It can be a XML file or in plain text file containing the key. The format of the keyfile won’t be discussed here, see [this link](http://web.archive.org/web/20160229111603/http://keepass.info/help/base/keys.html)), whereas in KeePass 2, you can still encrypt the database with a passphrase and/or a keyfile, but also with Windows User Account.

#### With a .kdb file

To generate the composite key for a .kdb file :

*   If you need a passphrase AND a keyfile to decrypt the file, you must hash (with SHA-256) the passphrase and the content of the keyfile (the “key” part). You must then concatenate the two hashes (the first one is the keyfile hash), then hash the result, and there you go, the composite key.  
    Pseudo-code : sha256( concat( sha256(passphrase), sha256(key_in_keyfile) ) ),
*   If you just need a passphrase OR a keyfile, you just need to hash it to get the composite key.

#### With a .kdbx file

NB : I don’t know how Windows User Account is used by KeePass and I’m not that interested in it, so if you need to know, I would recommend to read KeePass 2 source code.

To generate the composite key for a .kdbx file :

1.  You must hash (with SHA-256) all credentials needed (passphrase, key of the keyfile, Windows User Account),
2.  You must then concatenate ALL hashed credentials in this order : passphrase, keyfile, Windows User Account,
3.  You must then hash the result of the concatenation. Even though you have just one credential (for example, just a passphrase), you still need to hash it twice (a first one for the step 1, and second one for the step 3). The result of the hash is the composite key

### Generate the final master key from the composite key

This part is the same for .kdb and .kdbx files. In both file format, you should have get from the header :

*   a Transform Seed,
*   a number N of Encryption Rounds ,
*   a Master Seed.

To generate the final master key, you first need to generate the transformed key :

1.  create an AES cipher, taking Transform Seed as its key/seed,
2.  initialize the transformed key value with the composite key value (transformed_key = composite_key),
3.  use this cipher to encrypt the transformed_key N times ( transformed_key = AES(transformed_key), N times),
4.  hash (with SHA-256) the transformed_key (transformed_key = sha256(transformed_key) ),
5.  concatenate the Master Seed to the transformed_key (transformed_key = concat(Master Seed, transformed_key) ),
6.  hash (with SHA-256) the transformed_key to get the final master key (final_master_key = sha256(transformed_key) ).

You now have the final master key, you can finally decrypt the database (the part of the file after the header for .kdb, and after the End of Header field for .kdbx).

## The structure of the decrypted database

NB : if the compression flag was set in the header, you have to unzip the data after decrypting it.

The decrypted data of the database is structured differently if it is a kdb or kdbx file.

If it’s a kdb file, you can directly parse the data as XML, there’s no problem.

If it’s a kdbx file, the data can’t be use directly. The database part of the file first start with 32 bytes, the ones you should compare with the ones in the field “Stream Start Bytes” to check if the final master key generated is correct.  
After these 32 bytes, the data is a succession of block (at least two) laid out like :

*   (4 bytes) Block ID : an integer giving the block ID, the first block must have an ID of 0,
*   (32 bytes) Block Hash : the hash of the Block Data (see after),
*   (4 bytes) Data Size : an integer giving the size of Block Data (see after),
*   (Data Size Bytes) Block Data : the data of this block.

In the end, Block Data should be hashed and checked with its corresponding Block Hash, then all Block Data should be concatenated ordered by their ID (it should be the same order as the one you read them). After the concatenation, the obtained data should be a XML document.

## Bonus kdbx : encrypted password in XML

With kdbx file, you can encrypt a password field in the XML document, before the encryption of all the XML document, so that the password is encrypted twice. To decrypt these password, you have to sequentially look in the XML document for “Value” node with the “Protected” attribute set to “True”.  
To decrypt the password, you should use the cipher declared in the “Inner Random Stream ID” field of the header, initiating it with the value of the field “Protected Stream Key” as its key/seed and the fixed IV “0xE830094B97205D2A”.  
First you need to pass the password through a base64decode function, then you decrypt the result with the cipher : because the cipher is a stream cipher, you need to XOR the result of the decryption with the result of the base64decode function to obtain the real password.

If I’m wrong somewhere in this article, please let me know so I can correct it.

Sources :

*   KeePass v1 and KeePass2 source code,
*   libkeepass implementation in Python : [link](http://web.archive.org/web/20160229111603/https://github.com/phpwutz/libkeepass),
*   A gist found randomly (of course at the end of the article….) : [link](http://web.archive.org/web/20160229111603/https://gist.github.com/msmuenchen/9318327).