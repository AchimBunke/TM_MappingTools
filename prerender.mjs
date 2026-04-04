import puppeteer from 'puppeteer';
import { spawn } from 'child_process';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

// Serve with a base path so <base href="/TM_MappingTools/"> resolves correctly
const server = spawn('npx', ['serve', '.', '-p', '5500', '--no-clipboard'], {
  stdio: 'ignore',
  detached: true
});

await new Promise(r => setTimeout(r, 3000));

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox'],
  headless: true
});

const routes = ['/'];

for (const route of routes) {
  console.log(`Prerendering ${route}...`);
  const page = await browser.newPage();

  page.on('console', msg => console.log('  [browser]', msg.text()));
  page.on('pageerror', err => console.log('  [page error]', err.message));

  // Intercept requests and rewrite /TM_MappingTools/* -> /*
  await page.setRequestInterception(true);
  page.on('request', req => {
    const url = req.url().replace('http://localhost:5500/TM_MappingTools', 'http://localhost:5500');
    req.continue({ url });
  });

  await page.goto(`http://localhost:5500/`, {
    waitUntil: 'networkidle0',
    timeout: 60000
  });

  try {
    await page.waitForFunction(
      () => {
        const app = document.getElementById('app');
        return app && !app.querySelector('svg.loading-progress');
      },
      { timeout: 30000 }
    );
    console.log('  ✅ Blazor finished rendering');
  } catch (e) {
    console.log('  ⚠️ Timed out waiting for Blazor - saving whatever is rendered');
  }

  const html = await page.content();
  await page.close();

  const outDir = join('.', route === '/' ? '' : route);
  mkdirSync(outDir, { recursive: true });
  writeFileSync(join(outDir, 'index.html'), html);
  console.log(`  ✅ Done: ${route}`);
}

await browser.close();
server.kill();
console.log('Prerendering complete.');
