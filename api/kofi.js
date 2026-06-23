import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

export default async function handler(req, res) {
  if (req.method !== 'POST') return res.status(405).send('Method not allowed');

  const data = JSON.parse(req.body.data);

  if (data.verification_token !== process.env.KOFI_TOKEN) {
    return res.status(401).send('Unauthorized');
  }

  // Generate key
  const key = 'TIMER-' + crypto.randomUUID().split('-').slice(0, 2).join('-').toUpperCase();

  // Save to Supabase
  await supabase.from('licenses').insert({
    email: data.email,
    key: key,
    activations: 0,
    max_activations: 3,
    device_ids: [],
  });

  // Send email via Brevo
  await fetch('https://api.brevo.com/v3/smtp/email', {
    method: 'POST',
    headers: {
      'api-key': process.env.BREVO_API_KEY,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      sender: { name: 'FishTimer', email: 'sleetsheet.st@gmail.com' },
      to: [{ email: data.email }],
      subject: 'Your FishTimer license key',
      htmlContent: `
        <h2>Thanks for your purchase!</h2>
        <p>Here is your license key:</p>
        <h1 style="letter-spacing: 2px;">${key}</h1>
        <p>Enter this key on the FishTimer site to unlock access.</p>
      `,
    }),
  });

  return res.status(200).send('OK');
}