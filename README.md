# DtlsCertsInstall

DtlsCertsInstall is a plugin for Among Us that allows you to add custom DTLS certificates and connect to custom regions with DTLS authentication.

## Introduction

This plugin is designed for scenarios where you need to integrate custom DTLS certificates into the Among Us client. By loading a custom certificate, you can securely connect to unofficial server regions and enjoy a more flexible multiplayer experience.

The plugin is built with 2023.11.28's gamelib but should be working on most other versions as well. (Tested on 17.0.0)

## How to Use

1. Place the plugin into the Among Us plugins directory (Among Us\BepInEx\plugins).
2. Configure the certificate parameter in the plugin's config file (see details below).
3. Start the game and connect to your custom server.

## Configuration

You need to fill in the DTLS certificate in the plugin's configuration file.  
**The example value in the config is Innersloth's official certificate. It is provided for format reference only¡ªdo not use it for any illegal purposes.**

### Certificate Format

- The certificate is a normal PEM file.
- Add `\r\n` at the beginning and end of the certificate.
- Each line of the certificate is merged into one line, and lines are separated by `\r\n`.

**Example:**
```
\r\n-----BEGIN CERTIFICATE-----\r\nMII... (content omitted) ...IDAQAB\r\n-----END CERTIFICATE-----\r\n
```

Please convert your own certificate into the format above before entering it into the config file.

## Notes

- Do not misuse the official certificate. Respect the game developers and the community rules.
- This plugin is for learning and custom server connection only. Please comply with all relevant laws and regulations.

## License

MIT License

---

Feel free to open an issue or PR if you have any questions or suggestions.