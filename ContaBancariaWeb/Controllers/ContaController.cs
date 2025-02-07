﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Repository;

namespace ContaBancariaWeb.Controllers
{
    public class ContaController : Controller
    {
        private readonly ContaDAO _contaDAO;
        private readonly ClienteDAO _clienteDAO;
        private readonly UserManager<ContaLogada> _userManager;
        private readonly SignInManager<ContaLogada> _signInManager;
        private readonly TransacaoDAO _transacaoDAO;

        public ContaController(ContaDAO contaDAO, ClienteDAO clienteDAO, UserManager<ContaLogada> userManager,
            SignInManager<ContaLogada> signInManager, TransacaoDAO transacaoDAO)
        {
            _contaDAO = contaDAO;
            _clienteDAO = clienteDAO;
            _signInManager = signInManager;
            _userManager = userManager;
            _transacaoDAO = transacaoDAO;
        }
        public IActionResult Index()
        {

            return View();
        }
        public IActionResult Criar()
        {
            if (TempData["ErroCliente"] != null) { ModelState.AddModelError("", TempData["ErroCliente"].ToString()); }
            if (TempData["ErroVInvalido"] != null) { ModelState.AddModelError("", TempData["ErroVInvalido"].ToString()); }
            if (TempData["ErroConta"] != null) { ModelState.AddModelError("", TempData["ErroConta"].ToString()); }
            Conta conta = new Conta();

            return View(conta);
        }
        [HttpPost]
        public async Task<IActionResult> Criar(Conta conta, string txtCpf)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ContaLogada uLogado = new ContaLogada
                    {
                        UserName = Convert.ToString(conta.NumeroConta),
                        Email = Convert.ToString(conta.NumeroConta)
                    };
                    IdentityResult result =
                        await _userManager.CreateAsync(uLogado, conta.Senha);
                    if (result.Succeeded)
                    {
                        Cliente cliente = new Cliente();
                        cliente = _clienteDAO.BuscarClientePorCpf(txtCpf);
                        if (cliente != null)
                        {
                            conta.Cliente = cliente;
                            Transacao t = new Transacao();
                            t.NumeroConta = conta.NumeroConta;
                            t.Descricao = "Saldo Inicial";
                            t.Valor = Convert.ToDouble(conta.Saldo);
                            _transacaoDAO.CadastrarTransacao(t);
                            conta.Transacoes.Add(t);
                            if (_contaDAO.CriarConta(conta))
                            {
                                return RedirectToAction("Index", "Cliente");
                            }
                            TempData["ErroConta"] = "Este cliente ja tem uma conta!";
                        }
                        else
                        {
                            TempData["ErroCliente"] = "Cliente não existe !";
                        }
                    }
                    AdicionarErros(result);
                }
                catch(Exception e)
                {
                    TempData["ErroVInvalido"] = "Valores Inválidos !";
                }
                
            }

            return RedirectToAction("Criar", "Conta");

        }
        private void AdicionarErros(IdentityResult result)
        {
            foreach (var erro in result.Errors)
            {
                ModelState.AddModelError("", erro.Description);
            }
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Cliente");
        }

        public IActionResult AcessarConta()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcessarConta(Conta conta)
        {
            var result = await _signInManager.PasswordSignInAsync
                (Convert.ToString(conta.NumeroConta), conta.Senha, false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Transacao");
            }
            ModelState.AddModelError("", "Falha no login!");
            return View(conta);
        }
        public IActionResult RecuperarConta()
        {
            return View();
        }
        [HttpPost]
        public IActionResult RecuperarConta(Cliente cliente, string senha)
        {

            if (ModelState.IsValid)
            {
                Conta c = new Conta();
                c.Cliente = cliente;
                c = _contaDAO.BuscarContaPorCpfCliente(c);
                if (c != null)
                {
                    if (c.Senha.Equals(senha))
                    {
                        ViewBag.Contah = Convert.ToString(c.NumeroConta);
                        return View();
                    }
                    ModelState.AddModelError("", "Senha Incorreta!");
                }
                else
                {
                    ModelState.AddModelError("", "Esta conta não existe!");
                }

            }
            return View();
        }
    }               
}