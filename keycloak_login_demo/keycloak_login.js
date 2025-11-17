// Goal: Implement a simple Keycloak OpenID Connect password grant login using axios.
// The script should POST to TOKEN_URL, send client_id, username, and password, and print the access_token.
// Then decode the JWT payload using jsonwebtoken and print user info.

require('dotenv').config();
const axios = require('axios');
const jwt = require('jsonwebtoken');

// Load configuration from .env file
const {
  REALM,
  TOKEN_URL,
  CLIENT_ID,
  USERNAME,
  PASSWORD
} = process.env;

// Validate required environment variables
if (!TOKEN_URL || !CLIENT_ID || !USERNAME || !PASSWORD) {
  console.error('❌ Error: Missing required environment variables');
  console.error('Please ensure TOKEN_URL, CLIENT_ID, USERNAME, and PASSWORD are set in .env file');
  process.exit(1);
}

/**
 * Perform Keycloak login using Resource Owner Password Credentials Grant
 */
async function keycloakLogin() {
  try {
    console.log('🔐 Starting Keycloak OIDC Login...');
    console.log(`📍 Realm: ${REALM}`);
    console.log(`📍 Token URL: ${TOKEN_URL}`);
    console.log(`📍 Client ID: ${CLIENT_ID}`);
    console.log(`📍 Username: ${USERNAME}`);
    console.log('');

    // Prepare request payload (URL-encoded form data)
    const params = new URLSearchParams();
    params.append('grant_type', 'password');
    params.append('client_id', CLIENT_ID);
    params.append('username', USERNAME);
    params.append('password', PASSWORD);

    // Make POST request to Keycloak token endpoint
    console.log('📡 Sending authentication request...');
    const response = await axios.post(TOKEN_URL, params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      }
    });

    // Extract tokens from response
    const {
      access_token,
      refresh_token,
      token_type,
      expires_in,
      refresh_expires_in,
      scope
    } = response.data;

    console.log('✅ Login successful!\n');
    console.log('📊 Token Response:');
    console.log(`   Token Type: ${token_type}`);
    console.log(`   Expires In: ${expires_in} seconds`);
    console.log(`   Refresh Expires In: ${refresh_expires_in} seconds`);
    console.log(`   Scope: ${scope}`);
    console.log('');

    // Display access token
    console.log('🔑 Access Token:');
    console.log(access_token);
    console.log('');

    // Decode JWT token (without verification for demo purposes)
    console.log('🔍 Decoding JWT Token...\n');
    const decoded = jwt.decode(access_token, { complete: true });

    if (decoded) {
      console.log('📋 JWT Header:');
      console.log(JSON.stringify(decoded.header, null, 2));
      console.log('');

      console.log('👤 JWT Payload (User Info):');
      console.log(JSON.stringify(decoded.payload, null, 2));
      console.log('');

      // Extract and display key user information
      const payload = decoded.payload;
      console.log('📌 Key User Information:');
      console.log(`   Subject (sub): ${payload.sub}`);
      console.log(`   Email: ${payload.email || 'N/A'}`);
      console.log(`   Preferred Username: ${payload.preferred_username || 'N/A'}`);
      console.log(`   Name: ${payload.name || 'N/A'}`);
      console.log(`   Given Name: ${payload.given_name || 'N/A'}`);
      console.log(`   Family Name: ${payload.family_name || 'N/A'}`);
      console.log(`   Realm Roles: ${payload.realm_access?.roles?.join(', ') || 'N/A'}`);
      console.log(`   Issued At: ${new Date(payload.iat * 1000).toLocaleString()}`);
      console.log(`   Expires At: ${new Date(payload.exp * 1000).toLocaleString()}`);
      console.log('');

      // Display refresh token
      if (refresh_token) {
        console.log('🔄 Refresh Token:');
        console.log(refresh_token);
        console.log('');
      }

      return {
        access_token,
        refresh_token,
        decoded: payload
      };
    } else {
      console.error('❌ Failed to decode JWT token');
      return null;
    }

  } catch (error) {
    console.error('❌ Login failed:');
    
    if (error.response) {
      // Server responded with error
      console.error(`   Status: ${error.response.status}`);
      console.error(`   Error: ${error.response.data.error || 'Unknown error'}`);
      console.error(`   Description: ${error.response.data.error_description || 'No description'}`);
      console.error('');
      console.error('📄 Full Response:');
      console.error(JSON.stringify(error.response.data, null, 2));
    } else if (error.request) {
      // Request was made but no response received
      console.error('   No response received from server');
      console.error('   Please check:');
      console.error('     - Network connectivity');
      console.error('     - TOKEN_URL is correct');
      console.error('     - Keycloak server is running');
    } else {
      // Error in request setup
      console.error(`   ${error.message}`);
    }
    
    process.exit(1);
  }
}

// Run the login function
if (require.main === module) {
  keycloakLogin()
    .then((result) => {
      if (result) {
        console.log('✅ Authentication complete!');
        console.log('💡 You can now use the access_token for API calls');
      }
    })
    .catch((error) => {
      console.error('Unexpected error:', error);
      process.exit(1);
    });
}

module.exports = { keycloakLogin };
