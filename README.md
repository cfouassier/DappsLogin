# Login to ASP.NET Core App using MetaMask

 | This piece of software is highly inspired by Michel Pisicoli's excellent article [Secure Asp.Net Core 2 API with Ethereum Blockchain](https://medium.com/coinmonks/secure-asp-net-core-2-api-with-ethereum-blockchain-88001d5ddb6) |
| --- | 
 | Please, read the Michele's article above, before continuing. |



![Demo](/ScreenShots/Demo.gif "Demo")

This code is just exploratory and it is made public in case it could help people. All kind comments are welcome to improve this solution. :) 

## Getting a JWT using MetaMask

### On the client side
 ```javascript
let account = null;
let accessToken = null;
//Will Start the metamask extension
const accounts = await ethereum.request({ method: 'eth_requestAccounts' });
const account = accounts[0];


// https://ethereum.stackexchange.com/questions/94434/metamask-eth-sign-cant-make-it-work
const message = "Hi there from DAppsLogin! Sign this message to prove you have access to this wallet and we will log you in. This will not cost you any Ether.To stop hackers using your wallet, here is a unique message ID they cannot guess:@Guid.NewGuid().ToString()";

let a = buffer.Buffer.from(message, 'utf8');
let msg = EthJS.Util.bufferToHex(a);
let b = EthJS.Util.keccak256(buffer.Buffer.from("\x19Ethereum Signed Message:\n" + message.length + message));
let hash = EthJS.Util.bufferToHex(b);


const signature = await ethereum.request({ method: 'personal_sign', params: [message, account] });

console.log(signature);

let loginData = {};
loginData.signer = account;
loginData.signature = signature;
loginData.message = msg;
loginData.hash = hash;

fetch('https://localhost:44343/Token/CreateToken', {
    method: 'POST',
    body: JSON.stringify(loginData),
    headers: {
        'Content-type': 'application/json'
    }
})
.../...
.then(data => {              
    accessToken = data.token;
    console.log('access token: ' + accessToken);
    lbl_connection_status.innerHTML = 'CONNECTED';
    ethereumButton.innerHTML = 'LOGGED';
})    
 ```
### On the server side
```csharp
[AllowAnonymous]
[HttpPost]
public async Task<IActionResult> CreateToken([FromBody] LoginUser login)
{
    var user = await Authenticate(login);

    if (user != null)
    {
        var tokenString = BuildToken(user);
        return Json(new { token = tokenString });
    }

    return Unauthorized();
}

private async Task<User> Authenticate(LoginUser login)
{
    User user = null;

    var signer = new Nethereum.Signer.MessageSigner();
    var account = signer.EcRecover(login.Hash.HexToByteArray(), login.Signature);

    if (account.ToLower().Equals(login.Signer.ToLower()))
    {
        // read user from DB or create a new one
        user = new User { Account = account };
    }

    return user;
}
```         

## Using JWT for Ajax call 
Making an ajax call using JWT token in the header
### Client
```javascript
secretButton.addEventListener('click', async () => {
    fetch('https://localhost:44343/Home/Secret', {
        method: 'POST',
        headers: {
            'Authorization': 'Bearer ' + accessToken,
            'Content-type': 'application/json'
        }
    })
    .then((response) => {
        if (response.status == 200) {
            return response.json();
        }
        else {
            status_label.innerHTML = `error with status ${response.status}`;
            throw `error with status ${response.status}`;
        }
    })
    .then(data => {
        for (let i = 0; i < data.length; i++) {
            data_list.innerHTML += '<li>' + data[i] + '</li>';
        }
        status_label.innerHTML = `DONE`;
    })
    .catch((exception) => {
        console.log(exception);
        status_label.innerHTML = exception;
    });
});
```     
### Server
```csharp
[HttpPost]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public IActionResult Secret()
{
    return Json( new string[] { "Secret 1", "Secret 2" });
}
 ``` 

## Using JWT with cookies

### Server
During the **CreateToken** call, we create a cookie. No implementation on the Client side

In *Startup.cs*, on each JwtBearerEvents's event the cookie is read and set in the context

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["jwt-dapps"];
                return Task.CompletedTask;
            }
        };
    });

    services.AddControllersWithViews();
}
```
And when creating the token, the cookie is added
```csharp
[AllowAnonymous]
[HttpPost]
public async Task<IActionResult> CreateToken([FromBody] LoginUser login)
{
    var user = await Authenticate(login);

    if (user != null)
    {
        var tokenString = BuildToken(user);

        this.Response.Cookies.Append("jwt-dapps", tokenString,
            new Microsoft.AspNetCore.Http.CookieOptions()
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddSeconds(30)
            }
        ); ;

        return Json(new { token = tokenString });
    }

    return Unauthorized();
}
```
Now we can use the normal Autorize behavior.
```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public IActionResult Privacy()
{
    return View();
}
```



Notes : 
- In this Proof Of Concept, there are no CORS configured, since the Issuer and the Audience are on the same server. 
- the ethereumjs-util.js file was generate using 
 ```powershell
 browserify main.js -s EthJS > ethereumjs-util.js
 ```
[Source -> How to browserify node module ethereumjs-util](https://www.mobilefish.com/developer/nodejs/nodejs_quickguide_browserify_ethereumjs_util.html)
- Concerning  the message for end user and some code, it is interresting to read : [Writing for blockchain: wallet signature request messages](https://medium.com/hackernoon/writing-for-blockchain-wallet-signature-request-messages-6ede721160d5) 
