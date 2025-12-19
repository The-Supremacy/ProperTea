module.exports = {
  "/bff": {
    target:
      process.env["services__landlord-bff__https__0"] ||
      process.env["services__landlord-bff__http__0"] ||
      process.env["BFF_URL"] ||
      "http://localhost:5000",
    secure: false,
    changeOrigin: true,
    logLevel: "debug",
  },
  "/api": {
    target:
      process.env["services__landlord-bff__https__0"] ||
      process.env["services__landlord-bff__http__0"] ||
      process.env["BFF_URL"] ||
      "http://localhost:5000",
    secure: false,
    changeOrigin: true,
    logLevel: "debug",
  },
};
