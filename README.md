### PowerShell command for downloading fresh spot.XXXX.json and index.XXXX.json:
```
powershell -command iwr https://website.spot.ec2.aws.a2z.com/spot.json -out spot.$(Get-Date -Format yyyyMMdd).json
powershell -command iwr https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/AmazonEC2/current/index.json -out index.$(Get-Date -Format yyyyMMdd).json
```

### sh command for downloading fresh spot.XXXX.json and index.XXXX.json:
```
curl -fL https://website.spot.ec2.aws.a2z.com/spot.json -o spot.$(date '+%Y%m%d').json
curl -fL https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/AmazonEC2/current/index.json -o index.$(date '+%Y%m%d').json
```
