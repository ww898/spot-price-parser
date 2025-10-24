# PowerShell command for downloading fresh spot.XXXX.json: 
```
powershell -command iwr https://website.spot.ec2.aws.a2z.com/spot.json -out spot.$(Get-Date -Format yyyyMMdd).json
```

